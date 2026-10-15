import { CommonModule } from '@angular/common';
import { Component, OnDestroy, OnInit } from '@angular/core';
import { MatTooltipModule } from '@angular/material/tooltip';
import { of, Subject, timer } from 'rxjs';
import { catchError, map, switchMap, takeUntil, timeout } from 'rxjs/operators';

import { IMediaServerStatus } from '../interfaces';
import { LandingPageService, StatusService } from '../services';

@Component({
    standalone: true,
    selector: 'app-system-status',
    templateUrl: './system-status.component.html',
    styleUrls: ['./system-status.component.scss'],
    imports: [
        CommonModule,
        MatTooltipModule
    ]
})
export class SystemStatusComponent implements OnInit, OnDestroy {
    public reqestraOnline: boolean | null = null;
    public mediaServerStatus: IMediaServerStatus | null = null;
    public mediaStatusAvailable = false;

    private readonly destroy$ = new Subject<void>();
    private readonly reqestraPollIntervalMs = 30 * 1000;
    private readonly mediaPollIntervalMs = 60 * 1000;
    private readonly reqestraTimeoutMs = 5 * 1000;
    private readonly mediaTimeoutMs = 20 * 1000;

    constructor(
        private readonly statusService: StatusService,
        private readonly landingPageService: LandingPageService) {
    }

    public ngOnInit(): void {
        timer(0, this.reqestraPollIntervalMs)
            .pipe(
                switchMap(() => this.statusService.getStatus().pipe(
                    timeout(this.reqestraTimeoutMs),
                    map(() => true),
                    catchError(() => of(false))
                )),
                takeUntil(this.destroy$)
            )
            .subscribe(online => {
                this.reqestraOnline = online;
                if (!online) {
                    this.mediaStatusAvailable = false;
                }
            });

        timer(0, this.mediaPollIntervalMs)
            .pipe(
                switchMap(() => this.landingPageService.getServerStatus().pipe(
                    timeout(this.mediaTimeoutMs),
                    catchError(() => of(null))
                )),
                takeUntil(this.destroy$)
            )
            .subscribe(status => {
                this.mediaServerStatus = status;
                this.mediaStatusAvailable = status !== null;
            });
    }

    public ngOnDestroy(): void {
        this.destroy$.next();
        this.destroy$.complete();
    }

    public get statusClass(): string {
        if (this.reqestraOnline === false) {
            return 'offline';
        }

        if (this.reqestraOnline === null) {
            return 'checking';
        }

        if (!this.mediaStatusAvailable || !this.mediaServerStatus || this.mediaServerStatus.totalServers === 0) {
            return 'online';
        }

        if (this.mediaServerStatus.partiallyDown) {
            return 'degraded';
        }

        if (this.mediaServerStatus.completelyDown) {
            return 'offline';
        }

        return 'online';
    }

    public get statusLabel(): string {
        if (this.reqestraOnline === false) {
            return 'Reqestra Offline';
        }

        if (this.reqestraOnline === null) {
            return 'Checking';
        }

        if (!this.mediaStatusAvailable || !this.mediaServerStatus || this.mediaServerStatus.totalServers === 0) {
            return 'Reqestra Online';
        }

        if (this.mediaServerStatus.partiallyDown) {
            const count = this.mediaServerStatus.serversUnavailable;
            return `${count} Server${count === 1 ? '' : 's'} Offline`;
        }

        if (this.mediaServerStatus.completelyDown) {
            return 'Media Offline';
        }

        return 'Systems Online';
    }

    public get statusTooltip(): string {
        if (this.reqestraOnline === false) {
            return 'Reqestra is not responding.';
        }

        if (this.reqestraOnline === null) {
            return 'Checking Reqestra and media server status...';
        }

        if (!this.mediaStatusAvailable || !this.mediaServerStatus) {
            return 'Reqestra is online. Media server status is temporarily unavailable.';
        }

        if (this.mediaServerStatus.totalServers === 0) {
            return 'Reqestra is online. No media servers are configured.';
        }

        return `Reqestra is online. ${this.mediaServerStatus.serversAvailable} of ${this.mediaServerStatus.totalServers} media servers are online.`;
    }
}
