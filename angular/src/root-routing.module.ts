import { NgModule } from '@angular/core';
import { Routes, RouterModule } from '@angular/router';

const routes: Routes = [
    {
        path: 'account',
        loadChildren: () => import('account/account.module').then((m) => m.AccountModule), // Lazy load account module
        data: { preload: true },
    },
    {
        path: 'app',
        loadChildren: () => import('app/app.module').then((m) => m.AppModule), // Lazy load the workspace shell
        data: { preload: true },
    },
    {
        // public site (prd.md E7) - must stay LAST: it owns '' and '**'
        path: '',
        loadChildren: () => import('app/public/public.module').then((m) => m.PublicModule),
        data: { preload: true },
    },
];

@NgModule({
    imports: [RouterModule.forRoot(routes)],
    exports: [RouterModule],
    providers: [],
})
export class RootRoutingModule {}
