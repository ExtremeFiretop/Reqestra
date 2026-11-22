/**
 * Convert a Media Cleanup HTTP 429 into an actionable explanation. Keep this
 * independent of Angular components so the fallback paths can be unit tested.
 */
export function mediaCleanupRateLimitMessage(error: unknown, context: "action" | "refresh"): string | null {
    const response = error as {
        status?: number;
        headers?: { get(name: string): string | null };
        error?: { message?: unknown; retryAfterSeconds?: unknown };
    } | null;
    if (response?.status !== 429) {
        return null;
    }

    // Prefer our own API's concise explanation. An upstream proxy may supply
    // HTML or a generic error, so never dump arbitrary response bodies to users.
    const apiMessage = response.error?.message;
    if (typeof apiMessage === "string" &&
        apiMessage.startsWith("Too many Media Cleanup") && apiMessage.length <= 250) {
        return apiMessage;
    }

    const header = response.headers?.get("Retry-After");
    const rawSeconds = header ?? response.error?.retryAfterSeconds;
    const seconds = rawSeconds !== undefined && rawSeconds !== null && rawSeconds !== ""
        ? Number(rawSeconds)
        : NaN;
    const retry = Number.isFinite(seconds) && seconds > 0
        ? `Please wait ${Math.ceil(seconds)} seconds before trying again.`
        : "Please wait a minute before trying again.";

    return context === "action"
        ? `Too many Media Cleanup actions in a short time. Your action was not saved. ${retry}`
        : `Too many Media Cleanup refreshes in a short time. ${retry}`;
}
