import { ChangeDetectorRef, Component, Injector, OnInit } from '@angular/core';
import { ActivatedRoute, RouterLink } from '@angular/router';
import { AppComponentBase } from '@shared/app-component-base';
import { PostServiceProxy, PublicPostDetailDto } from '@shared/service-proxies/service-proxies';
import { CommentThreadComponent } from '../comment-thread/comment-thread.component';
import { UpvoteButtonComponent } from '@shared/upvote/upvote-button.component';
import { DatePipe } from '@angular/common';
import { MarkdownPipe } from '@shared/pipes/markdown.pipe';
import { LocalizePipe } from '@shared/pipes/localize.pipe';

/**
 * Public post reading view with its comment discussion (prd.md E7-S2).
 * Anonymous-readable by slug; interactive controls (upvote, comment) adapt
 * to the viewer's authentication and bans.
 */
@Component({
    templateUrl: './post-detail.component.html',
    standalone: true,
    imports: [
        CommentThreadComponent,
        UpvoteButtonComponent,
        RouterLink,
        DatePipe,
        MarkdownPipe,
        LocalizePipe,
    ],
})
export class PostDetailComponent extends AppComponentBase implements OnInit {
    post: PublicPostDetailDto | undefined;
    loading = true;

    constructor(
        injector: Injector,
        private _postService: PostServiceProxy,
        private _route: ActivatedRoute,
        private cd: ChangeDetectorRef
    ) {
        super(injector);
    }

    ngOnInit(): void {
        const slug = this._route.snapshot.paramMap.get('slug') ?? '';

        this._postService
            .getPublicPostBySlug(slug)
            .subscribe(
                (result) => {
                    this.post = result;
                },
                () => {
                    this.post = undefined; // unknown or unpublished slug
                }
            )
            .add(() => {
                this.loading = false;
                this.cd.detectChanges();
            });
    }
}
