import { ChangeDetectorRef, Component, EventEmitter, Injector, Output } from '@angular/core';
import { BsModalRef } from 'ngx-bootstrap/modal';
import { AppComponentBase } from '@shared/app-component-base';
import { PostServiceProxy, RejectPostInput } from '@shared/service-proxies/service-proxies';
import { FormsModule } from '@angular/forms';
import { AbpModalHeaderComponent } from '../../../shared/components/modal/abp-modal-header.component';
import { AbpValidationSummaryComponent } from '../../../shared/components/validation/abp-validation.summary.component';
import { AbpModalFooterComponent } from '../../../shared/components/modal/abp-modal-footer.component';
import { LocalizePipe } from '@shared/pipes/localize.pipe';

@Component({
    templateUrl: './reject-post-dialog.component.html',
    standalone: true,
    imports: [FormsModule, AbpModalHeaderComponent, AbpValidationSummaryComponent, AbpModalFooterComponent, LocalizePipe],
})
export class RejectPostDialogComponent extends AppComponentBase {
    @Output() onSave = new EventEmitter<any>();

    saving = false;
    // Set by the dialog opener before the dialog is shown.
    id!: string;
    reason = '';

    constructor(
        injector: Injector,
        private _postService: PostServiceProxy,
        public bsModalRef: BsModalRef,
        private cd: ChangeDetectorRef
    ) {
        super(injector);
    }

    save(): void {
        this.saving = true;

        this._postService
            .reject(
                new RejectPostInput({
                    id: this.id,
                    reason: this.reason,
                })
            )
            .subscribe({
                next: () => {
                    this.notify.info(this.l('PostRejected'));
                    this.bsModalRef.hide();
                    this.onSave.emit();
                },
                error: () => {
                    this.saving = false;
                    this.cd.detectChanges();
                },
            });
    }
}
