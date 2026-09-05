import { Injectable } from '@angular/core';
import {
    ApplicationInfoDto,
    GetCurrentLoginInformationsOutput,
    SessionServiceProxy,
    UserLoginInfoDto,
} from '@shared/service-proxies/service-proxies';

@Injectable()
export class AppSessionService {
    // Set by init() before the app renders (APP_INITIALIZER awaits it).
    private _user!: UserLoginInfoDto;
    private _application!: ApplicationInfoDto;

    constructor(private _sessionService: SessionServiceProxy) {}

    get application(): ApplicationInfoDto {
        return this._application;
    }

    get user(): UserLoginInfoDto {
        return this._user;
    }

    get userId(): number | null {
        return this.user ? this.user.id : null;
    }

    getShownLoginName(): string {
        return this._user.userName;
    }

    init(): Promise<boolean> {
        return new Promise<boolean>((resolve, reject) => {
            this._sessionService
                .getCurrentLoginInformations()
                .toPromise()
                .then(
                    (result: GetCurrentLoginInformationsOutput) => {
                        this._application = result.application;
                        this._user = result.user;

                        resolve(true);
                    },
                    (err) => {
                        reject(err);
                    }
                );
        });
    }
}
