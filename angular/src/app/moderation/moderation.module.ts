import { NgModule } from '@angular/core';
import { CommonModule } from '@angular/common';
import { SharedModule } from '@shared/shared.module';
import { ModerationRoutingModule } from './moderation-routing.module';
import { ModerationComponent } from './moderation.component';

@NgModule({
    imports: [CommonModule, SharedModule, ModerationRoutingModule, ModerationComponent],
})
export class ModerationModule {}
