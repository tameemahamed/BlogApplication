import { NgModule } from '@angular/core';
import { RouterModule } from '@angular/router';
import { ModerationComponent } from './moderation.component';

@NgModule({
    imports: [RouterModule.forChild([{ path: '', component: ModerationComponent }])],
    exports: [RouterModule],
})
export class ModerationRoutingModule {}
