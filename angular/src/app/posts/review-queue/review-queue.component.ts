import { ChangeDetectorRef, Component, Injector, OnInit } from '@angular/core';
import { BsModalService, BsModalRef } from 'ngx-bootstrap/modal';
import { appModuleAnimation } from '@shared/animations/routerTransition';
import { PagedListingComponentBase } from 'shared/paged-listing-component-base';
import { PostServiceProxy, PostDto, PostDtoPagedResultDto } from '@shared/service-proxies/service-proxies';
import { RejectPostDialogComponent } from '../reject-post/reject-post-dialog.component';
import { LazyLoadEvent } from 'primeng/api';
import { AbpPaginationControlsComponent } from '@shared/components/pagination/abp-pagination-controls.component';
import { NgxPaginationModule } from 'ngx-pagination';
import { DatePipe, SlicePipe } from '@angular/common';
import { LocalizePipe } from '@shared/pipes/localize.pipe';

@Component({
    templateUrl: './review-queue.component.html',
    animations: [appModuleAnimation()],
    standalone: true,
    imports: [NgxPaginationModule, AbpPaginationControlsComponent, DatePipe, SlicePipe, LocalizePipe],
})
export class ReviewQueueComponent extends PagedListingComponentBase<PostDto> implements OnInit {
    posts: PostDto[] = [];

    constructor(
        injector: Injector,
        private _postService: PostServiceProxy,
        private _modalService: BsModalService,
        cd: ChangeDetectorRef
    ) {
        super(injector, cd);
    }

    ngOnInit(): void {
        this.list();
    }

    list(event?: LazyLoadEvent): void {
        this.isTableLoading = true;

        this._postService
            .getPendingReviewPosts((this.pageNumber - 1) * this.pageSize, this.pageSize)
            .subscribe((result: PostDtoPagedResultDto) => {
                this.posts = result.items ?? [];
                this.totalItems = result.totalCount;
            })
            .add(() => {
                // Zoneless app: async state updates need an explicit change-detection
                // pass; the teardown runs on success and error alike.
                this.isTableLoading = false;
                this.cd.detectChanges();
            });
    }

    delete(post: PostDto): void {
        abp.message.confirm(this.l('PostDeleteWarningMessage', post.title), undefined, (result: boolean) => {
            if (result) {
                this._postService.delete(post.id).subscribe(() => {
                    abp.notify.success(this.l('SuccessfullyDeleted'));
                    this.refresh();
                });
            }
        });
    }

    approvePost(post: PostDto): void {
        abp.message.confirm(this.l('PostApproveConfirmMessage', post.title), undefined, (result: boolean) => {
            if (result) {
                this._postService.approve(post.id).subscribe(() => {
                    abp.notify.success(this.l('PostApproved'));
                    this.refresh();
                });
            }
        });
    }

    rejectPost(post: PostDto): void {
        const dialog: BsModalRef = this._modalService.show(RejectPostDialogComponent, {
            initialState: {
                id: post.id,
            },
        });
        dialog.content.onSave.subscribe(() => this.refresh());
    }

    pageChanged(page: number): void {
        this.pageNumber = page;
        this.list();
    }
}
