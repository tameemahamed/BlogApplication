import { ChangeDetectorRef, Component, EventEmitter, Injector, OnInit, Output } from '@angular/core';
import { BsModalRef } from 'ngx-bootstrap/modal';
import { AppComponentBase } from '@shared/app-component-base';
import { PostServiceProxy, UpdatePostInput } from '@shared/service-proxies/service-proxies';
import { FormsModule } from '@angular/forms';
import { AbpModalHeaderComponent } from '../../../shared/components/modal/abp-modal-header.component';
import { AbpValidationSummaryComponent } from '../../../shared/components/validation/abp-validation.summary.component';
import { AbpModalFooterComponent } from '../../../shared/components/modal/abp-modal-footer.component';
import { LocalizePipe } from '@shared/pipes/localize.pipe';

@Component({
    templateUrl: './edit-post-dialog.component.html',
    standalone: true,
    imports: [FormsModule, AbpModalHeaderComponent, AbpValidationSummaryComponent, AbpModalFooterComponent, LocalizePipe],
})
export class EditPostDialogComponent extends AppComponentBase implements OnInit {
    @Output() onSave = new EventEmitter<any>();

    saving = false;
    // Set by the dialog opener before the dialog is shown.
    id!: string;
    post = new UpdatePostInput();
    status: number | undefined;
    rejectionReason: string | undefined;

    constructor(
        injector: Injector,
        private _postService: PostServiceProxy,
        public bsModalRef: BsModalRef,
        private cd: ChangeDetectorRef
    ) {
        super(injector);
    }

    ngOnInit(): void {
        this._postService.getForEdit(this.id).subscribe((result) => {
            this.post = new UpdatePostInput({
                id: result.id,
                title: result.title ?? '',
                excerpt: result.excerpt,
                contentMarkdown: result.contentMarkdown ?? '',
            });
            this.status = result.status;
            this.rejectionReason = result.rejectionReason;
            this.cd.detectChanges();
        });
    }

    save(): void {
        this.saving = true;

        this._postService.update(this.post).subscribe(
            () => {
                this.notify.info(this.l('PostUpdated'));
                this.bsModalRef.hide();
                this.onSave.emit();
            },
            () => {
                this.saving = false;
                this.cd.detectChanges();
            }
        );
    }

    getStatusLabel(): string {
        const keys = [
            'PostStatusDraft',
            'PostStatusPendingReview',
            'PostStatusApproved',
            'PostStatusRejected',
            'PostStatusArchived',
        ];
        return this.l(keys[this.status ?? 0]);
    }
}
