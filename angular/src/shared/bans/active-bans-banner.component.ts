import { ChangeDetectorRef, Component, Injector, OnInit } from '@angular/core';
import { AppComponentBase } from '@shared/app-component-base';
import { ActiveUserBanDto, UserBanServiceProxy } from '@shared/service-proxies/service-proxies';
import { LocalizePipe } from '@shared/pipes/localize.pipe';

/**
 * The current user's active restrictions with their reasons (prd.md E6-S3),
 * read from the ban ledger. Rendered on the interactive areas so a banned
 * user sees why their controls disappeared - distinguishing "banned" from
 * "not permitted" (prd.md E6-S4).
 */
@Component({
    selector: 'active-bans-banner',
    templateUrl: './active-bans-banner.component.html',
    standalone: true,
    imports: [LocalizePipe],
})
export class ActiveBansBannerComponent extends AppComponentBase implements OnInit {
    activeBans: ActiveUserBanDto[] = [];

    constructor(
        injector: Injector,
        private _userBanService: UserBanServiceProxy,
        private cd: ChangeDetectorRef
    ) {
        super(injector);
    }

    ngOnInit(): void {
        if (!this.appSession.userId) {
            return;
        }

        this._userBanService
            .getMyActiveBans()
            .subscribe((result) => {
                this.activeBans = result ?? [];
            })
            .add(() => {
                this.cd.detectChanges();
            });
    }

    // BanType.Comment = 0, Reply = 1, Upvote = 2 (int-backed on the wire)
    banTypeLabel(banType: number): string {
        switch (banType) {
            case 0:
                return this.l('BannedFromCommenting');
            case 1:
                return this.l('BannedFromReplying');
            default:
                return this.l('BannedFromUpvoting');
        }
    }
}
