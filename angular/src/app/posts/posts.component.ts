import { ChangeDetectorRef, Component, Injector, OnInit } from '@angular/core';
import { BsModalService, BsModalRef } from 'ngx-bootstrap/modal';
import { appModuleAnimation } from '@shared/animations/routerTransition';
import { PagedListingComponentBase } from 'shared/paged-listing-component-base';
import { PostServiceProxy, PostDto, PostDtoPagedResultDto } from '@shared/service-proxies/service-proxies';
import { CreatePostDialogComponent } from './create-post/create-post-dialog.component';
import { EditPostDialogComponent } from './edit-post/edit-post-dialog.component';
import { LazyLoadEvent } from 'primeng/api';
import { AbpPaginationControlsComponent } from '@shared/components/pagination/abp-pagination-controls.component';
import { NgxPaginationModule } from 'ngx-pagination';
import { DatePipe } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { RouterLink } from '@angular/router';
import { LocalizePipe } from '@shared/pipes/localize.pipe';

@Component({
    templateUrl: './posts.component.html',
    animations: [appModuleAnimation()],
    standalone: true,
    imports: [FormsModule, NgxPaginationModule, AbpPaginationControlsComponent, DatePipe, RouterLink, LocalizePipe],
})
export class PostsComponent extends PagedListingComponentBase<PostDto> implements OnInit {
    posts: PostDto[] = [];
    statusFilter: number | undefined;

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
            .getMyPosts(this.statusFilter, (this.pageNumber - 1) * this.pageSize, this.pageSize)
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

    createPost(): void {
        const dialog: BsModalRef = this._modalService.show(CreatePostDialogComponent, { class: 'modal-lg' });
        dialog.content.onSave.subscribe(() => this.refresh());
    }

    editPost(post: PostDto): void {
        const dialog: BsModalRef = this._modalService.show(EditPostDialogComponent, {
            class: 'modal-lg',
            initialState: {
                id: post.id,
            },
        });
        dialog.content.onSave.subscribe(() => this.refresh());
    }

    submitPost(post: PostDto): void {
        abp.message.confirm(this.l('PostSubmitConfirmMessage', post.title), undefined, (result: boolean) => {
            if (result) {
                this._postService.submit(post.id).subscribe(() => {
                    abp.notify.success(this.l('PostSubmitted'));
                    this.refresh();
                });
            }
        });
    }

    archivePost(post: PostDto): void {
        abp.message.confirm(this.l('PostArchiveWarningMessage', post.title), undefined, (result: boolean) => {
            if (result) {
                this._postService.archive(post.id).subscribe(() => {
                    abp.notify.success(this.l('PostArchived'));
                    this.refresh();
                });
            }
        });
    }

    statusFilterChanged(): void {
        this.pageNumber = 1;
        this.list();
    }

    pageChanged(page: number): void {
        this.pageNumber = page;
        this.list();
    }

    getStatusLabel(status: number): string {
        const keys = [
            'PostStatusDraft',
            'PostStatusPendingReview',
            'PostStatusApproved',
            'PostStatusRejected',
            'PostStatusArchived',
        ];
        return this.l(keys[status]);
    }
}
