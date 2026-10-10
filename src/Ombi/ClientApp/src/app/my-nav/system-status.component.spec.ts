import { describe, expect, it } from 'vitest';
import { SystemStatusComponent } from './system-status.component';

function createComponent() {
    const statusService = { getStatus: () => { throw new Error('not used in state tests'); } };
    const landingPageService = { getServerStatus: () => { throw new Error('not used in state tests'); } };
    return new SystemStatusComponent(statusService as any, landingPageService as any);
}

describe('SystemStatusComponent', () => {
    it('shows Reqestra offline when the application probe fails', () => {
        const component = createComponent();
        component.reqestraOnline = false;

        expect(component.statusClass).toBe('offline');
        expect(component.statusLabel).toBe('Reqestra Offline');
    });

    it('shows all systems online when every configured media server is available', () => {
        const component = createComponent();
        component.reqestraOnline = true;
        component.mediaStatusAvailable = true;
        component.mediaServerStatus = {
            serversAvailable: 2,
            serversUnavailable: 0,
            partiallyDown: false,
            completelyDown: false,
            fullyAvailable: true,
            totalServers: 2
        };

        expect(component.statusClass).toBe('online');
        expect(component.statusLabel).toBe('Systems Online');
        expect(component.statusTooltip).toContain('2 of 2 media servers are online');
    });

    it('shows a degraded state when one media server is unavailable', () => {
        const component = createComponent();
        component.reqestraOnline = true;
        component.mediaStatusAvailable = true;
        component.mediaServerStatus = {
            serversAvailable: 1,
            serversUnavailable: 1,
            partiallyDown: true,
            completelyDown: false,
            fullyAvailable: false,
            totalServers: 2
        };

        expect(component.statusClass).toBe('degraded');
        expect(component.statusLabel).toBe('1 Server Offline');
    });

    it('shows media offline while keeping Reqestra itself online', () => {
        const component = createComponent();
        component.reqestraOnline = true;
        component.mediaStatusAvailable = true;
        component.mediaServerStatus = {
            serversAvailable: 0,
            serversUnavailable: 1,
            partiallyDown: false,
            completelyDown: true,
            fullyAvailable: false,
            totalServers: 1
        };

        expect(component.statusClass).toBe('offline');
        expect(component.statusLabel).toBe('Media Offline');
        expect(component.statusTooltip).toContain('Reqestra is online');
    });

    it('shows Reqestra online when media status is not available', () => {
        const component = createComponent();
        component.reqestraOnline = true;
        component.mediaStatusAvailable = false;

        expect(component.statusClass).toBe('online');
        expect(component.statusLabel).toBe('Reqestra Online');
        expect(component.statusTooltip).toContain('Media server status is temporarily unavailable');
    });
});
