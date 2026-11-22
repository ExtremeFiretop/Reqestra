import { describe, expect, it } from "vitest";
import { mediaCleanupRateLimitMessage } from "./media-cleanup-rate-limit";

describe("Media Cleanup rate-limit messaging", () => {
    it("displays the API's friendly response", () => {
        const message = "Too many Media Cleanup actions in a short time. Your action was not saved. Please wait 17 seconds before trying again.";
        expect(mediaCleanupRateLimitMessage({ status: 429, error: { message } }, "action")).toBe(message);
    });

    it("uses Retry-After when an upstream proxy removes the response body", () => {
        const response = { status: 429, headers: { get: (name: string) => name === "Retry-After" ? "22" : null } };
        expect(mediaCleanupRateLimitMessage(response, "action"))
            .toContain("Your action was not saved. Please wait 22 seconds");
    });

    it("uses the JSON retry interval when the header is unavailable", () => {
        expect(mediaCleanupRateLimitMessage({ status: 429, error: { retryAfterSeconds: 12 } }, "refresh"))
            .toContain("refreshes in a short time. Please wait 12 seconds");
    });

    it("falls back safely for malformed and non-JSON 429 responses", () => {
        const response = { status: 429, headers: { get: () => "invalid" }, error: { message: "<html>Server Error</html>" } };
        const message = mediaCleanupRateLimitMessage(response, "action");
        expect(message).toContain("Please wait a minute");
        expect(message).not.toContain("<html>");
    });

    it("does not misclassify unrelated errors as rate limits", () => {
        expect(mediaCleanupRateLimitMessage({ status: 500, error: { message: "Error" } }, "action")).toBeNull();
        expect(mediaCleanupRateLimitMessage(null, "refresh")).toBeNull();
    });
});
