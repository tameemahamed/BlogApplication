import { ChangeDetectorRef, Component, Injector, OnInit } from '@angular/core';
import { RouterLink } from '@angular/router';
import { AppComponentBase } from '@shared/app-component-base';
import { PostServiceProxy, PublicPostListDto } from '@shared/service-proxies/service-proxies';
import { AbpPaginationControlsComponent } from '@shared/components/pagination/abp-pagination-controls.component';
import { DatePipe } from '@angular/common';
import { LocalizePipe } from '@shared/pipes/localize.pipe';

/**
 * Public feed of approved posts (prd.md E7-S1): newest first by default,
 * switchable to top (most upvoted). The endpoint is anonymous and returns
 * approved posts only, so no unpublished content can leak (prd.md A12).
 */
@Component({
    selector: 'public-home',
    templateUrl: './public-home.component.html',
    standalone: true,
    imports: [RouterLink, AbpPaginationControlsComponent, DatePipe, LocalizePipe],
})
export class PublicHomeComponent extends AppComponentBase implements OnInit {
    posts: PublicPostListDto[] = [];
    loading = true;
    loadFailed = false;
    sortByUpvotes = false;
    pageNumber = 1;
    pageSize = 10;
    totalItems = 0;

    constructor(
        injector: Injector,
        private _postService: PostServiceProxy,
        private cd: ChangeDetectorRef
    ) {
        super(injector);
    }

    ngOnInit(): void {
        this.load();
    }

    load(): void {
        this.loading = true;
        this.loadFailed = false;

        this._postService
            .getPublicPosts(this.sortByUpvotes, (this.pageNumber - 1) * this.pageSize, this.pageSize)
            .subscribe(
                (result) => {
                    this.posts = result.items ?? [];
                    this.totalItems = result.totalCount;
                },
                () => {
                    this.posts = [];
                    this.totalItems = 0;
                    this.loadFailed = true;
                }
            )
            .add(() => {
                // Zoneless app: async state updates need an explicit
                // change-detection pass; teardown runs on success and error.
                this.loading = false;
                this.cd.detectChanges();
            });
    }

    sortBy(top: boolean): void {
        if (this.sortByUpvotes === top) {
            return;
        }

        this.sortByUpvotes = top;
        this.pageNumber = 1;
        this.load();
    }

    pageChanged(page: number): void {
        this.pageNumber = page;
        this.load();
    }
}
