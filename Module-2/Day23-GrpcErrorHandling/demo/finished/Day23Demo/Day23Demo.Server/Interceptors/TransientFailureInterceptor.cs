// ============================================================================
// TEST HARNESS — NOT TEACHING CODE
// ============================================================================
// This interceptor injects transient Unavailable failures on the first N unary
// gRPC calls so that /verify-demo can exercise the client-side retry policy
// end-to-end. It is inert unless the DAY23_INJECT_TRANSIENT_FAILURES env var
// is set to "1". In production runs, instructor F5 demos, and student
// exploration this file has zero effect.
//
// Why this exists: Day 23 client Program.cs configures a gRPC RetryPolicy
// (MaxAttempts=3, InitialBackoff=500ms, 2x multiplier, RetryableStatusCodes
// = { Unavailable }). Asserting that the policy is *registered* is trivial;
// proving it actually *works* requires a server that fails deterministically
// on the first couple of attempts and then succeeds. That's this class.
//
// Why an interceptor and not middleware: gRPC responses are HTTP/2 200 with
// status in trailers. Emitting Unavailable from raw middleware would mean
// hand-writing grpc-status trailers and racing against the gRPC framework's
// own trailer write. An interceptor that throws RpcException(Unavailable)
// lets the framework format the response exactly as a real transient
// failure would look, which is what the client's retry policy is trained to
// recognize. The interceptor also cleanly scopes to UnaryServerHandler so
// streaming methods (StreamProducts, BatchAddProducts) are untouched.
//
// Env vars (both read from IConfiguration so they work via appsettings.json
// OR environment variables):
//   DAY23_INJECT_TRANSIENT_FAILURES = "1" to activate. Any other value or
//     unset means the interceptor is never registered.
//   DAY23_INJECT_FAIL_ATTEMPTS      = number of initial attempts to fail
//     before the interceptor stops injecting and lets calls pass through.
//     Defaults to 2, which pairs with the client's MaxAttempts=3 so the
//     third attempt succeeds. Setting this >= MaxAttempts will cause the
//     verify-demo run to fail, which is the expected behavior if someone
//     breaks the retry policy in the client.
//
// Counter semantics: a single process-wide counter across all unary methods.
// Good enough for /verify-demo which exercises one call flow per run and
// kills the server process at the end. Do NOT use this class for any
// scenario that needs per-method or per-client isolation.
// ============================================================================

using Grpc.Core;
using Grpc.Core.Interceptors;

namespace Day23Demo.Server.Interceptors;

public class TransientFailureInterceptor : Interceptor
{
    private static int _attemptCount;
    private readonly int _failAttempts;
    private readonly ILogger<TransientFailureInterceptor> _logger;

    public TransientFailureInterceptor(
        IConfiguration configuration,
        ILogger<TransientFailureInterceptor> logger)
    {
        _logger = logger;
        var raw = configuration["DAY23_INJECT_FAIL_ATTEMPTS"];
        _failAttempts = int.TryParse(raw, out var parsed) ? parsed : 2;
        _logger.LogWarning(
            "TransientFailureInterceptor ACTIVE — will fail the first {FailAttempts} unary call(s) with Unavailable. This is verify-demo test-harness behavior.",
            _failAttempts);
    }

    public override async Task<TResponse> UnaryServerHandler<TRequest, TResponse>(
        TRequest request,
        ServerCallContext context,
        UnaryServerMethod<TRequest, TResponse> continuation)
    {
        var attempt = Interlocked.Increment(ref _attemptCount);
        if (attempt <= _failAttempts)
        {
            _logger.LogInformation(
                "TransientFailureInterceptor: injecting Unavailable (attempt {Attempt}/{FailPlusOne}) on {Method}",
                attempt,
                _failAttempts + 1,
                context.Method);
            throw new RpcException(new Status(
                StatusCode.Unavailable,
                $"transient failure injected for retry test (attempt {attempt}/{_failAttempts + 1})"));
        }

        _logger.LogInformation(
            "TransientFailureInterceptor: passing through attempt {Attempt} on {Method}",
            attempt,
            context.Method);
        return await continuation(request, context);
    }
}
