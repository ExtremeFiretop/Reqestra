import { PlatformLocation, APP_BASE_HREF } from "@angular/common";
import { HttpClient } from "@angular/common/http";
import { Injectable, Inject } from "@angular/core";

import { Observable } from "rxjs";

import { ServiceHelpers } from "../service.helpers";

import { IPlexOAuthAccessToken } from "../../interfaces";

@Injectable({
    providedIn: 'root'
})
export class PlexOAuthService extends ServiceHelpers {
    constructor(http: HttpClient, @Inject(APP_BASE_HREF) href:string) {
        super(http, "/api/v1/PlexOAuth/", href);
    }

    public oAuth(pollToken: string): Observable<IPlexOAuthAccessToken> {
        return this.http.post<IPlexOAuthAccessToken>(this.url, { pollToken }, {headers: this.headers});
    }
}
