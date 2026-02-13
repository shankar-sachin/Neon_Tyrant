using System.Runtime.InteropServices;

namespace NeonTyrant;

public static class NativePhysicsBridge
{
    [StructLayout(LayoutKind.Sequential)]
    public struct NtWorldConfig
    {
        public float Gravity;
        public float JumpVelocity;
        public float MaxFallSpeed;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct NtInput
    {
        public float CurrentVelocityY;
        public int JumpPressed;
        public int IsGrounded;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct NtWorldSnapshot
    {
        public float VelocityY;
        public float MoveY;
        public int Grounded;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct NtAabb
    {
        public float X;
        public float Y;
        public float W;
        public float H;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct NtCollisionResult
    {
        public float ResolvedDx;
        public float ResolvedDy;
        public int CollidedX;
        public int CollidedY;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct NtDashResult
    {
        public float MoveX;
        public int CooldownMs;
        public int DidDash;
    }

    private static readonly bool NativeAvailable;

    static NativePhysicsBridge()
    {
        NativeAvailable = File.Exists(Path.Combine(AppContext.BaseDirectory, "physics.dll"));
    }

    public static bool Initialize()
    {
        if (!NativeAvailable)
        {
            return false;
        }

        var cfg = new NtWorldConfig
        {
            Gravity = 28f,
            JumpVelocity = -10.5f,
            MaxFallSpeed = 16f
        };
        return nt_init_world(ref cfg) == 0;
    }

    public static void Shutdown()
    {
        if (NativeAvailable)
        {
            nt_shutdown_world();
        }
    }

    public static NtWorldSnapshot Step(float dt, float velocityY, bool jumpPressed, bool grounded)
    {
        if (!NativeAvailable)
        {
            var nextVy = grounded && jumpPressed ? -10.5f : MathF.Min(16f, velocityY + 28f * dt);
            return new NtWorldSnapshot
            {
                VelocityY = nextVy,
                MoveY = nextVy * dt,
                Grounded = grounded ? 1 : 0
            };
        }

        var input = new NtInput
        {
            CurrentVelocityY = velocityY,
            JumpPressed = jumpPressed ? 1 : 0,
            IsGrounded = grounded ? 1 : 0
        };
        nt_step_world(dt, input, out var snapshot);
        return snapshot;
    }

    public static NtCollisionResult Resolve(NtAabb player, float dx, float dy, NtAabb obstacle)
    {
        if (!NativeAvailable)
        {
            return new NtCollisionResult
            {
                ResolvedDx = dx,
                ResolvedDy = dy
            };
        }

        nt_resolve_player_move(ref player, dx, dy, ref obstacle, out var result);
        return result;
    }

    public static bool Intersects(NtAabb a, NtAabb b)
    {
        if (!NativeAvailable)
        {
            return !(a.X + a.W <= b.X || a.X >= b.X + b.W || a.Y + a.H <= b.Y || a.Y >= b.Y + b.H);
        }

        nt_check_hazard_overlap(ref a, ref b, out var hit);
        return hit == 1;
    }

    public static bool BossHit(NtAabb attack, NtAabb boss)
    {
        if (!NativeAvailable)
        {
            return Intersects(attack, boss);
        }

        nt_boss_hit_test(ref attack, ref boss, out var hit);
        return hit == 1;
    }

    public static NtDashResult ComputeDash(float dt, bool dashPressed, float facingDir, int cooldownMs)
    {
        if (!NativeAvailable)
        {
            var nextCooldown = Math.Max(0, cooldownMs - (int)(dt * 1000));
            if (dashPressed && nextCooldown == 0)
            {
                return new NtDashResult
                {
                    MoveX = (facingDir < 0 ? -1f : 1f) * 2.6f,
                    CooldownMs = 900,
                    DidDash = 1
                };
            }

            return new NtDashResult
            {
                MoveX = 0,
                CooldownMs = nextCooldown,
                DidDash = 0
            };
        }

        nt_compute_dash(dt, dashPressed ? 1 : 0, facingDir, cooldownMs, out var result);
        return result;
    }

    [DllImport("physics.dll", CallingConvention = CallingConvention.Cdecl)]
    private static extern int nt_init_world(ref NtWorldConfig cfg);

    [DllImport("physics.dll", CallingConvention = CallingConvention.Cdecl)]
    private static extern int nt_step_world(float dt, NtInput input, out NtWorldSnapshot snapshot);

    [DllImport("physics.dll", CallingConvention = CallingConvention.Cdecl)]
    private static extern int nt_resolve_player_move(ref NtAabb player, float dx, float dy, ref NtAabb obstacle, out NtCollisionResult result);

    [DllImport("physics.dll", CallingConvention = CallingConvention.Cdecl)]
    private static extern int nt_check_hazard_overlap(ref NtAabb player, ref NtAabb hazard, out int hit);

    [DllImport("physics.dll", CallingConvention = CallingConvention.Cdecl)]
    private static extern int nt_boss_hit_test(ref NtAabb attack, ref NtAabb boss, out int bossHit);

    [DllImport("physics.dll", CallingConvention = CallingConvention.Cdecl)]
    private static extern int nt_compute_dash(float dt, int dashPressed, float facingDir, int cooldownMs, out NtDashResult result);

    [DllImport("physics.dll", CallingConvention = CallingConvention.Cdecl)]
    private static extern void nt_shutdown_world();
}
