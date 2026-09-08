import { ChangeDetectorRef, Component, Injector, OnInit } from '@angular/core';
import { AppComponentBase } from '@shared/app-component-base';
import {
    BanUserInput,
    UnbanInput,
    UserBanDto,
    UserBanServiceProxy,
} from '@shared/service-proxies/service-proxies';
import { BsModalRef } from 'ngx-bootstrap/modal';
import { FormsModule } from '@angular/forms';
import { DatePipe } from '@angular/common';
import { AbpModalHeaderComponent } from '@shared/components/modal/abp-modal-header.component';
import { LocalizePipe } from '@shared/pipes/localize.pipe';

/**
 * Ban management for one user (prd.md E6-S2): the active bans with unban,
 * plus the ban form (type + reason). Opened from the users table.
 */
@Component({
    selector: 'app-user-bans-dialog',
    templateUrl: './user-bans-dialog.component.html',
    standalone: true,
    imports: [FormsModule, DatePipe, AbpModalHeaderComponent, LocalizePipe],
})
export class UserBansDialogComponent extends AppComponentBase implements OnInit {
    // Set by the dialog opener before the dialog is shown.
    id!: number;
    userName = '';

    bans: UserBanDto[] = [];
    loading = true;
    saving = false;
    banTypeSelection: boolean[] = [false, false, false];
    newBanReason = '';

    constructor(
        injector: Injector,
        private _userBanService: UserBanServiceProxy,
        public bsModalRef: BsModalRef,
        private cd: ChangeDetectorRef
    ) {
        super(injector);
    }

    ngOnInit(): void {
        this.load();
    }

    load(): void {
        this.loading = true;
        this._userBanService
            .getUserBans(this.id, true, 0, 100)
            .subscribe((result) => {
                this.bans = result.items ?? [];
            })
            .add(() => {
                this.loading = false;
                this.cd.detectChanges();
            });
    }

    availableBanTypes(): number[] {
        const bannedTypes = this.bans.map((b) => b.banType);
        return [0, 1, 2].filter((t) => bannedTypes.indexOf(t) === -1);
    }

    // A single ban action may cover several abilities (prd.md E6-S1).
    selectedBanTypes(): number[] {
        return this.availableBanTypes().filter((t) => this.banTypeSelection[t]);
    }

    // BanType.Comment = 0, Reply = 1, Upvote = 2 (int-backed on the wire)
    banTypeLabel(banType: number): string {
        switch (banType) {
            case 0:
                return this.l('BanTypeComment');
            case 1:
                return this.l('BanTypeReply');
            default:
                return this.l('BanTypeUpvote');
        }
    }

    ban(): void {
        const banTypes = this.selectedBanTypes();
        if (banTypes.length === 0 || !this.newBanReason.trim()) {
            return;
        }

        this.saving = true;
        this._userBanService
            .ban(
                new BanUserInput({
                    userId: this.id,
                    banTypes: banTypes,
                    reason: this.newBanReason,
                })
            )
            .subscribe(
                () => {
                    this.notify.success(this.l('BanSuccess'));
                    this.newBanReason = '';
                    this.banTypeSelection = [false, false, false];
                    this.load();
                },
                () => {}
            )
            .add(() => {
                this.saving = false;
                this.cd.detectChanges();
            });
    }

    unban(userBan: UserBanDto): void {
        abp.message.confirm(this.l('UnbanWarningMessage'), undefined, (result: boolean) => {
            if (result) {
                this._userBanService
                    .unban(
                        new UnbanInput({
                            userId: this.id,
                            banType: userBan.banType,
                        })
                    )
                    .subscribe(() => {
                        this.notify.success(this.l('UnbanSuccess'));
                        this.load();
                    });
            }
        });
    }
}
