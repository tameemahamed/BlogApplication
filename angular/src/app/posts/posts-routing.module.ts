import { NgModule } from '@angular/core';
import { RouterModule, Routes } from '@angular/router';
import { AppRouteGuard } from '@shared/auth/auth-route-guard';
import { PostsComponent } from './posts.component';
import { ReviewQueueComponent } from './review-queue/review-queue.component';
import { PostDetailComponent } from './detail/post-detail.component';

const routes: Routes = [
    {
        path: '',
        component: PostsComponent,
        pathMatch: 'full',
        canActivate: [AppRouteGuard],
        data: { permission: 'Pages.Blog.Posts.Create' },
    },
    {
        path: 'review',
        component: ReviewQueueComponent,
        canActivate: [AppRouteGuard],
        data: { permission: 'Pages.Blog.Posts.Approve' },
    },
    {
        path: 'detail/:slug',
        component: PostDetailComponent,
        canActivate: [AppRouteGuard],
    },
];

@NgModule({
    imports: [RouterModule.forChild(routes)],
    exports: [RouterModule],
})
export class PostsRoutingModule {}
