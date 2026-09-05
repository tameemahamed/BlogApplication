import { NgModule } from '@angular/core';
import { CommonModule } from '@angular/common';
import { SharedModule } from '@shared/shared.module';
import { PostsRoutingModule } from './posts-routing.module';
import { PostsComponent } from './posts.component';
import { CreatePostDialogComponent } from './create-post/create-post-dialog.component';
import { EditPostDialogComponent } from './edit-post/edit-post-dialog.component';
import { RejectPostDialogComponent } from './reject-post/reject-post-dialog.component';
import { ReviewQueueComponent } from './review-queue/review-queue.component';

@NgModule({
    imports: [
        CommonModule,
        SharedModule,
        PostsRoutingModule,
        PostsComponent,
        CreatePostDialogComponent,
        EditPostDialogComponent,
        RejectPostDialogComponent,
        ReviewQueueComponent,
    ],
})
export class PostsModule {}
