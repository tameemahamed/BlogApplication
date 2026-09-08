import { NgModule } from '@angular/core';
import { CommonModule } from '@angular/common';
import { SharedModule } from '@shared/shared.module';
import { PostDetailComponent } from '../posts/detail/post-detail.component';
import { NotFoundComponent } from './not-found/not-found.component';
import { PublicHomeComponent } from './public-home/public-home.component';
import { PublicLayoutComponent } from './public-layout/public-layout.component';
import { PublicRoutingModule } from './public-routing.module';

@NgModule({
    imports: [
        CommonModule,
        SharedModule,
        PublicRoutingModule,
        PublicLayoutComponent,
        PublicHomeComponent,
        NotFoundComponent,
        PostDetailComponent,
    ],
})
export class PublicModule {}
