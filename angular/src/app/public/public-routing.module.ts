import { NgModule } from '@angular/core';
import { RouterModule, Routes } from '@angular/router';
import { PostDetailComponent } from '../posts/detail/post-detail.component';
import { NotFoundComponent } from './not-found/not-found.component';
import { PublicHomeComponent } from './public-home/public-home.component';
import { PublicLayoutComponent } from './public-layout/public-layout.component';

/**
 * Public (anonymous-friendly) routes, mounted at the site root (prd.md E7).
 * No AppRouteGuard here: browsing and reading require no authentication.
 */
const routes: Routes = [
    {
        path: '',
        component: PublicLayoutComponent,
        children: [
            {
                path: '',
                component: PublicHomeComponent,
                pathMatch: 'full',
            },
            {
                path: 'posts/:slug',
                component: PostDetailComponent,
            },
            {
                path: '**',
                component: NotFoundComponent,
            },
        ],
    },
];

@NgModule({
    imports: [RouterModule.forChild(routes)],
    exports: [RouterModule],
})
export class PublicRoutingModule {}
