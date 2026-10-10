import { describe, expect, it, vi } from "vitest";
import { of } from "rxjs";
import { RequestType } from "../../../../../interfaces";
import { TvRequestsPanelComponent } from "./tv-requests-panel.component";

function createComponent() {
    const requestService = {};
    const requestService2 = {
        reprocessRequest: vi.fn().mockReturnValue(of({ result: true })),
    };
    const messageService = {
        send: vi.fn(),
        sendRequestEngineResultError: vi.fn(),
    };
    const dialog = {
        open: vi.fn().mockReturnValue({ afterClosed: () => of({ profileId: 9 }) }),
    };
    const translateService = {
        instant: vi.fn((key: string) => key),
    };
    const auth = {
        claims: vi.fn().mockReturnValue({ name: "owner" }),
    };

    const component = new TvRequestsPanelComponent(
        requestService as any,
        requestService2 as any,
        messageService as any,
        dialog as any,
        translateService as any,
        auth as any,
    );
    component.canSelectQualityProfile = true;

    return { component, requestService2, messageService, dialog };
}

describe("TvRequestsPanelComponent profile-aware retry", () => {
    it("reprocesses an owned available downloading request with the selected profile", async () => {
        const { component, requestService2 } = createComponent();
        const request = {
            id: 51,
            approved: true,
            available: true,
            downloading: true,
            denied: false,
            qualityOverride: 3,
            requestedUser: { userName: "owner" },
            parentRequest: { qualityOverride: 3 },
        } as any;

        expect(component.canRetryWithProfile(request)).toBe(true);
        await component.reProcessRequestWithProfile(request);

        expect(requestService2.reprocessRequest).toHaveBeenCalledWith(51, RequestType.tvShow, false, 9);
        expect(request.qualityOverride).toBe(9);
        expect(request.parentRequest.qualityOverride).toBe(9);
    });

    it("does not offer profile retry for another user's request", () => {
        const { component } = createComponent();
        const request = {
            approved: true,
            available: false,
            denied: false,
            requestedUser: { userName: "other-user" },
        } as any;

        expect(component.canRetryWithProfile(request)).toBe(false);
    });

    it("does not offer profile retry while pending or denied", () => {
        const { component } = createComponent();
        const request = {
            approved: false,
            available: false,
            denied: false,
            requestedUser: { userName: "owner" },
        } as any;

        expect(component.canRetryWithProfile(request)).toBe(false);

        request.approved = true;
        request.denied = true;
        expect(component.canRetryWithProfile(request)).toBe(false);
    });
});
