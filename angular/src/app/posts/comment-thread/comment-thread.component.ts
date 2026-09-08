import { ChangeDetectorRef, Component, Injector, Input, OnInit } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { AppComponentBase } from '@shared/app-component-base';
import {
    BanUserInput,
    CommentDto,
    CommentServiceProxy,
    CommentThreadDto,
    CreateCommentInput,
    CreateReplyInput,
    TopLevelCommentDto,
    UpdateCommentInput,
    UserBanServiceProxy,
} from '@shared/service-proxies/service-proxies';
import { MarkdownInputComponent } from '@shared/markdown-input/markdown-input.component';
import { UpvoteButtonComponent } from '@shared/upvote/upvote-button.component';
import { ActiveBansBannerComponent } from '@shared/bans/active-bans-banner.component';
import { AbpPaginationControlsComponent } from '@shared/components/pagination/abp-pagination-controls.component';
import { DatePipe } from '@angular/common';
import { MarkdownPipe } from '@shared/pipes/markdown.pipe';
import { LocalizePipe } from '@shared/pipes/localize.pipe';

/**
 * The discussion of one post (prd.md Epic 4): paged top-level comments
 * (newest first) with their reply subtrees flattened one level deep -
 * deeper replies carry an @author reference (prd.md A5).
 */
@Component({
    selector: 'comment-thread',
    templateUrl: './comment-thread.component.html',
    standalone: true,
    imports: [
        AbpPaginationControlsComponent,
        FormsModule,
        MarkdownInputComponent,
        UpvoteButtonComponent,
        ActiveBansBannerComponent,
        DatePipe,
        MarkdownPipe,
        LocalizePipe,
    ],
})
export class CommentThreadComponent extends AppComponentBase implements OnInit {
    @Input() postId!: string;
    @Input() postAuthorId!: number;

    thread: CommentThreadDto | undefined;
    loading = true;
    submitting = false;
    pageNumber = 1;
    pageSize = 10;

    newComment = '';
    replyingTo: string | undefined;
    replyText = '';
    editingCommentId: string | undefined;
    editValue = '';

    // inline ban form (prd.md E6-S2): opened from a comment's action row,
    // targets that comment's author; several abilities can be banned at once
    banningCommentId: string | undefined;
    readonly banTypes = [0, 1, 2];
    banTypeSelection: boolean[] = [false, false, false];
    banReason = '';
    banning = false;

    constructor(
        injector: Injector,
        private _commentService: CommentServiceProxy,
        private _userBanService: UserBanServiceProxy,
        private cd: ChangeDetectorRef
    ) {
        super(injector);
    }

    ngOnInit(): void {
        this.load();
    }

    load(): void {
        this.loading = true;

        this._commentService
            .getCommentThread(this.postId, (this.pageNumber - 1) * this.pageSize, this.pageSize)
            .subscribe((result) => {
                this.thread = result;
            })
            .add(() => {
                this.loading = false;
                this.cd.detectChanges();
            });
    }

    // ---- permission helpers -------------------------------------------------

    canComment(): boolean {
        return this.permission.isGranted('Pages.Blog.Comments.Create');
    }

    canReply(): boolean {
        return this.permission.isGranted('Pages.Blog.Replies.Create');
    }

    canEdit(comment: CommentDto): boolean {
        return (
            this.permission.isGranted('Pages.Blog.Comments.Edit') &&
            comment.userId === this.appSession.userId
        );
    }

    // prd.md A10: own comment, the post's author on their own post, or
    // moderator/admin (approval permission) on any comment
    canDelete(comment: CommentDto): boolean {
        return (
            comment.userId === this.appSession.userId ||
            this.postAuthorId === this.appSession.userId ||
            this.permission.isGranted('Pages.Blog.Posts.Approve')
        );
    }

    // prd.md E6-S2: moderators/admins ban users directly from comment context
    canManageBans(): boolean {
        return this.permission.isGranted('Pages.Blog.Bans.Manage');
    }

    // ---- actions ------------------------------------------------------------

    submitComment(): void {
        if (!this.newComment.trim()) {
            return;
        }

        this.submitting = true;
        this._commentService
            .createComment(
                new CreateCommentInput({
                    postId: this.postId,
                    contentMarkdown: this.newComment,
                })
            )
            .subscribe(
                () => {
                    this.newComment = '';
                    this.pageNumber = 1;
                    this.load();
                },
                () => {}
            )
            .add(() => {
                this.submitting = false;
                this.cd.detectChanges();
            });
    }

    startReply(comment: CommentDto): void {
        this.replyingTo = comment.id;
        this.replyText = '';
    }

    cancelReply(): void {
        this.replyingTo = undefined;
        this.replyText = '';
    }

    submitReply(): void {
        if (!this.replyingTo || !this.replyText.trim()) {
            return;
        }

        this.submitting = true;
        this._commentService
            .createReply(
                new CreateReplyInput({
                    parentCommentId: this.replyingTo,
                    contentMarkdown: this.replyText,
                })
            )
            .subscribe(
                () => {
                    this.cancelReply();
                    this.load();
                },
                () => {}
            )
            .add(() => {
                this.submitting = false;
                this.cd.detectChanges();
            });
    }

    startEdit(comment: CommentDto): void {
        this.editingCommentId = comment.id;
        this.editValue = comment.contentMarkdown ?? '';
    }

    cancelEdit(): void {
        this.editingCommentId = undefined;
        this.editValue = '';
    }

    saveEdit(): void {
        if (!this.editingCommentId || !this.editValue.trim()) {
            return;
        }

        this.submitting = true;
        this._commentService
            .updateComment(
                new UpdateCommentInput({
                    id: this.editingCommentId,
                    contentMarkdown: this.editValue,
                })
            )
            .subscribe(
                () => {
                    this.notify.success(this.l('CommentUpdated'));
                    this.cancelEdit();
                    this.load();
                },
                () => {}
            )
            .add(() => {
                this.submitting = false;
                this.cd.detectChanges();
            });
    }

    deleteComment(comment: CommentDto): void {
        abp.message.confirm(this.l('CommentDeleteWarningMessage'), undefined, (result: boolean) => {
            if (result) {
                this._commentService.deleteComment(comment.id).subscribe(() => {
                    this.notify.success(this.l('CommentDeleted'));
                    this.load();
                });
            }
        });
    }

    // ---- bans (prd.md E6-S2) -------------------------------------------------

    startBan(comment: CommentDto): void {
        this.banningCommentId = comment.id;
        this.banTypeSelection = [false, false, false];
        this.banReason = '';
    }

    cancelBanForm(): void {
        this.banningCommentId = undefined;
        this.banTypeSelection = [false, false, false];
        this.banReason = '';
    }

    selectedBanTypes(): number[] {
        return this.banTypes.filter((t) => this.banTypeSelection[t]);
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

    submitBan(comment: CommentDto): void {
        const banTypes = this.selectedBanTypes();
        if (banTypes.length === 0 || !this.banReason.trim()) {
            return;
        }

        this.banning = true;
        this._userBanService
            .ban(
                new BanUserInput({
                    userId: comment.userId,
                    banTypes: banTypes,
                    reason: this.banReason,
                })
            )
            .subscribe(
                () => {
                    this.notify.success(this.l('BanSuccess'));
                    this.cancelBanForm();
                },
                () => {}
            )
            .add(() => {
                this.banning = false;
                this.cd.detectChanges();
            });
    }

    // ---- display helpers ----------------------------------------------------

    // The @author prefix for replies-to-replies rendered flat in the thread
    parentUserName(reply: CommentDto, topLevel: TopLevelCommentDto): string {
        if (reply.parentCommentId === topLevel.id) {
            return topLevel.userName!;
        }

        const parent = topLevel.replies?.find((r) => r.id === reply.parentCommentId);
        return parent ? parent.userName! : '';
    }

    pageChanged(page: number): void {
        this.pageNumber = page;
        this.load();
    }
}
