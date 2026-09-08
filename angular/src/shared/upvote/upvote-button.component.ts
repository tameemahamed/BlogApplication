import { ChangeDetectorRef, Component, Injector, Input, OnChanges } from '@angular/core';
import { RouterLink } from '@angular/router';
import { AppComponentBase } from '@shared/app-component-base';
import { ToggleUpvoteInput, UpvoteServiceProxy } from '@shared/service-proxies/service-proxies';
import { LocalizePipe } from '@shared/pipes/localize.pipe';

/**
 * Upvote toggle for posts, comments and replies (prd.md Epic 5). Renders the
 * live count for everyone; the toggle button appears only for users holding
 * Pages.Blog.Upvotes.Toggle (granted to every registered role by default -
 * bans revoke it via user-level prohibition). Anonymous visitors see the
 * count plus a login call-to-action (prd.md E7-S3).
 */
@Component({
    selector: 'upvote-button',
    templateUrl: './upvote-button.component.html',
    standalone: true,
    imports: [RouterLink, LocalizePipe],
})
export class UpvoteButtonComponent extends AppComponentBase implements OnChanges {
    @Input() targetType: 'post' | 'comment' = 'post';
    @Input() targetId = '';
    @Input() upvoteCount = 0;
    @Input() upvoted: boolean | undefined;

    count = 0;
    isUpvoted = false;
    toggling = false;

    constructor(
        injector: Injector,
        private _upvoteService: UpvoteServiceProxy,
        private cd: ChangeDetectorRef
    ) {
        super(injector);
    }

    // Re-sync when the parent refetches fresh server data; local toggle state
    // survives unrelated parent re-renders because the bound input values do
    // not change until a reload replaces them.
    ngOnChanges(): void {
        this.count = this.upvoteCount;
        this.isUpvoted = this.upvoted === true;
    }

    canUpvote(): boolean {
        return this.permission.isGranted('Pages.Blog.Upvotes.Toggle');
    }

    isAuthenticated(): boolean {
        return !!this.appSession.userId;
    }

    toggle(): void {
        if (this.toggling) {
            return;
        }

        this.toggling = true;
        this._upvoteService
            .toggle(
                new ToggleUpvoteInput({
                    // UpvoteTargetType.Post = 0, UpvoteTargetType.Comment = 1
                    // (int-backed on the wire, comments and replies both count
                    // as comments)
                    targetType: this.targetType === 'post' ? 0 : 1,
                    targetId: this.targetId,
                })
            )
            .subscribe((result) => {
                this.count = result.upvoteCount;
                this.isUpvoted = result.upvoted;
            })
            .add(() => {
                this.toggling = false;
                this.cd.detectChanges();
            });
    }
}
