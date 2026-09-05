import { ChangeDetectorRef, Component, EventEmitter, Injector, Output } from '@angular/core';
import { BsModalRef } from 'ngx-bootstrap/modal';
import { AppComponentBase } from '@shared/app-component-base';
import { PostServiceProxy, CreatePostInput } from '@shared/service-proxies/service-proxies';
import { FormsModule } from '@angular/forms';
import { AbpModalHeaderComponent } from '../../../shared/components/modal/abp-modal-header.component';
import { AbpValidationSummaryComponent } from '../../../shared/components/validation/abp-validation.summary.component';
import { AbpModalFooterComponent } from '../../../shared/components/modal/abp-modal-footer.component';
import { LocalizePipe } from '@shared/pipes/localize.pipe';

@Component({
    templateUrl: './create-post-dialog.component.html',
    standalone: true,
    imports: [FormsModule, AbpModalHeaderComponent, AbpValidationSummaryComponent, AbpModalFooterComponent, LocalizePipe],
})
export class CreatePostDialogComponent extends AppComponentBase {
    @Output() onSave = new EventEmitter<any>();

    saving = false;
    post = new CreatePostInput();

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

        this._postService.create(this.post).subscribe(
            () => {
                this.notify.info(this.l('PostCreated'));
                this.bsModalRef.hide();
                this.onSave.emit();
            },
            () => {
                this.saving = false;
                this.cd.detectChanges();
            }
        );
    }
}
