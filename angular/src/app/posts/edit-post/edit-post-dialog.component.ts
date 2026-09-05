import { ChangeDetectorRef, Component, EventEmitter, Injector, OnInit, Output } from '@angular/core';
import { BsModalRef } from 'ngx-bootstrap/modal';
import { AppComponentBase } from '@shared/app-component-base';
import { PostServiceProxy, UpdatePostInput } from '@shared/service-proxies/service-proxies';
import { FormsModule } from '@angular/forms';
import { AbpModalHeaderComponent } from '../../../shared/components/modal/abp-modal-header.component';
import { AbpValidationSummaryComponent } from '../../../shared/components/validation/abp-validation.summary.component';
import { MarkdownPipe } from '@shared/pipes/markdown.pipe';
import { LocalizePipe } from '@shared/pipes/localize.pipe';

@Component({
    templateUrl: './edit-post-dialog.component.html',
    standalone: true,
    imports: [FormsModule, AbpModalHeaderComponent, AbpValidationSummaryComponent, MarkdownPipe, LocalizePipe],
})
export class EditPostDialogComponent extends AppComponentBase implements OnInit {
    @Output() onSave = new EventEmitter<any>();

    saving = false;
    previewMode = false;
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

    // Draft, Rejected and Archived posts can be submitted for review;
    // PendingReview posts can only be saved, and saving an Approved post
    // sends it back to review (prd.md A4)
    get canSubmit(): boolean {
        return this.status === 0 || this.status === 3 || this.status === 4;
    }

    save(submitForReview: boolean): void {
        this.saving = true;

        this._postService.update(this.post).subscribe(
            () => {
                if (submitForReview) {
                    this._postService.submit(this.post.id).subscribe(
                        () => this.completeAfterSave(this.l('PostSubmitted')),
                        // the changes were saved; submit can be retried from the workspace
                        () => this.completeAfterSave(this.l('PostUpdated'))
                    );
                } else {
                    this.completeAfterSave(this.l('PostUpdated'));
                }
            },
            () => {
                this.saving = false;
                this.cd.detectChanges();
            }
        );
    }

    private completeAfterSave(message: string): void {
        this.notify.info(message);
        this.bsModalRef.hide();
        this.onSave.emit();
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
