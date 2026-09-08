import { ChangeDetectorRef, Component, Injector, OnInit } from '@angular/core';
import { AppComponentBase } from '@shared/app-component-base';
import {
    CommentServiceProxy,
    ModerationCommentDto,
    ModerationCommentDtoPagedResultDto,
    PostDto,
    PostDtoPagedResultDto,
    PostServiceProxy,
    UnbanInput,
    UserBanDto,
    UserBanDtoPagedResultDto,
    UserBanServiceProxy,
} from '@shared/service-proxies/service-proxies';
import { BsModalRef, BsModalService } from 'ngx-bootstrap/modal';
import { appModuleAnimation } from '@shared/animations/routerTransition';
import { RejectPostDialogComponent } from '../posts/reject-post/reject-post-dialog.component';
import { AbpPaginationControlsComponent } from '@shared/components/pagination/abp-pagination-controls.component';
import { DatePipe, SlicePipe } from '@angular/common';
import { LocalizePipe } from '@shared/pipes/localize.pipe';

/**
 * Moderation overview (prd.md E6-S6): the banned-users list with unban, the
 * recent comments feed with delete actions, and the pending review queue.
 */
@Component({
    templateUrl: './moderation.component.html',
    animations: [appModuleAnimation()],
    standalone: true,
    imports: [AbpPaginationControlsComponent, DatePipe, SlicePipe, LocalizePipe],
})
export class ModerationComponent extends AppComponentBase implements OnInit {
    bans: UserBanDto[] = [];
    bansLoading = true;
    bansPageNumber = 1;
    bansPageSize = 10;
    bansTotalItems = 0;

    comments: ModerationCommentDto[] = [];
    commentsLoading = true;
    commentsPageNumber = 1;
    commentsPageSize = 10;
    commentsTotalItems = 0;

    posts: PostDto[] = [];
    postsLoading = true;
    postsPageNumber = 1;
    postsPageSize = 10;
    postsTotalItems = 0;

    constructor(
        injector: Injector,
        private _userBanService: UserBanServiceProxy,
        private _commentService: CommentServiceProxy,
        private _postService: PostServiceProxy,
        private _modalService: BsModalService,
        private cd: ChangeDetectorRef
    ) {
        super(injector);
    }

    ngOnInit(): void {
        this.loadBans();
        this.loadComments();
        this.loadPosts();
    }

    loadBans(): void {
        this.bansLoading = true;
        this._userBanService
            .getUserBans(undefined, true, (this.bansPageNumber - 1) * this.bansPageSize, this.bansPageSize)
            .subscribe((result: UserBanDtoPagedResultDto) => {
                this.bans = result.items ?? [];
                this.bansTotalItems = result.totalCount;
            })
            .add(() => {
                this.bansLoading = false;
                this.cd.detectChanges();
            });
    }

    loadComments(): void {
        this.commentsLoading = true;
        this._commentService
            .getRecentCommentsForModeration(
                (this.commentsPageNumber - 1) * this.commentsPageSize,
                this.commentsPageSize
            )
            .subscribe((result: ModerationCommentDtoPagedResultDto) => {
                this.comments = result.items ?? [];
                this.commentsTotalItems = result.totalCount;
            })
            .add(() => {
                this.commentsLoading = false;
                this.cd.detectChanges();
            });
    }

    loadPosts(): void {
        this.postsLoading = true;
        this._postService
            .getPendingReviewPosts((this.postsPageNumber - 1) * this.postsPageSize, this.postsPageSize)
            .subscribe((result: PostDtoPagedResultDto) => {
                this.posts = result.items ?? [];
                this.postsTotalItems = result.totalCount;
            })
            .add(() => {
                this.postsLoading = false;
                this.cd.detectChanges();
            });
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

    unban(userBan: UserBanDto): void {
        abp.message.confirm(this.l('UnbanWarningMessage'), undefined, (result: boolean) => {
            if (result) {
                this._userBanService
                    .unban(
                        new UnbanInput({
                            userId: userBan.userId,
                            banType: userBan.banType,
                        })
                    )
                    .subscribe(() => {
                        this.notify.success(this.l('UnbanSuccess'));
                        this.loadBans();
                    });
            }
        });
    }

    deleteComment(comment: ModerationCommentDto): void {
        abp.message.confirm(this.l('CommentDeleteWarningMessage'), undefined, (result: boolean) => {
            if (result) {
                this._commentService.deleteComment(comment.id).subscribe(() => {
                    this.notify.success(this.l('CommentDeleted'));
                    this.loadComments();
                });
            }
        });
    }

    approvePost(post: PostDto): void {
        abp.message.confirm(this.l('PostApproveConfirmMessage', post.title), undefined, (result: boolean) => {
            if (result) {
                this._postService.approve(post.id).subscribe(() => {
                    this.notify.success(this.l('PostApproved'));
                    this.loadPosts();
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
        dialog.content.onSave.subscribe(() => this.loadPosts());
    }

    deletePost(post: PostDto): void {
        abp.message.confirm(this.l('PostDeleteWarningMessage', post.title), undefined, (result: boolean) => {
            if (result) {
                this._postService.delete(post.id).subscribe(() => {
                    this.notify.success(this.l('SuccessfullyDeleted'));
                    this.loadPosts();
                });
            }
        });
    }

    bansPageChanged(page: number): void {
        this.bansPageNumber = page;
        this.loadBans();
    }

    commentsPageChanged(page: number): void {
        this.commentsPageNumber = page;
        this.loadComments();
    }

    postsPageChanged(page: number): void {
        this.postsPageNumber = page;
        this.loadPosts();
    }
}
