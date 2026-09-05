import { ChangeDetectorRef, Component, EventEmitter, Injector, Output } from '@angular/core';
import { BsModalRef } from 'ngx-bootstrap/modal';
import { AppComponentBase } from '@shared/app-component-base';
import { PostServiceProxy, CreatePostInput } from '@shared/service-proxies/service-proxies';
import { FormsModule } from '@angular/forms';
import { AbpModalHeaderComponent } from '../../../shared/components/modal/abp-modal-header.component';
import { AbpValidationSummaryComponent } from '../../../shared/components/validation/abp-validation.summary.component';
import { MarkdownPipe } from '@shared/pipes/markdown.pipe';
import { LocalizePipe } from '@shared/pipes/localize.pipe';

@Component({
    templateUrl: './create-post-dialog.component.html',
    standalone: true,
    imports: [
        FormsModule,
        AbpModalHeaderComponent,
        AbpValidationSummaryComponent,
        MarkdownPipe,
        LocalizePipe,
    ],
})
export class CreatePostDialogComponent extends AppComponentBase {
    @Output() onSave = new EventEmitter<any>();

    saving = false;
    previewMode = false;
    post = new CreatePostInput();

    constructor(
        injector: Injector,
        private _postService: PostServiceProxy,
        public bsModalRef: BsModalRef,
        private cd: ChangeDetectorRef
    ) {
        super(injector);
    }

    save(submitForReview: boolean): void {
        this.saving = true;

        this._postService.create(this.post).subscribe(
            (result) => {
                if (submitForReview) {
                    this._postService.submit(result.id).subscribe(
                        () => this.completeAfterSave(this.l('PostSubmitted')),
                        // the draft was created; submit can be retried from the workspace
                        () => this.completeAfterSave(this.l('PostCreated'))
                    );
                } else {
                    this.completeAfterSave(this.l('PostCreated'));
                }
            },
            () => this.saveFailed()
        );
    }

    private completeAfterSave(message: string): void {
        this.notify.info(message);
        this.bsModalRef.hide();
        this.onSave.emit();
    }

    private saveFailed(): void {
        this.saving = false;
        this.cd.detectChanges();
    }
}
