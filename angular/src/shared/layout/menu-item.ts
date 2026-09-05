export class MenuItem {
    id?: number;
    parentId?: number;
    label: string;
    route: string;
    permissionName?: string;
    isActive?: boolean;
    isCollapsed?: boolean;
    children?: MenuItem[];

    constructor(label: string, route: string, permissionName?: string, children?: MenuItem[]) {
        this.label = label;
        this.route = route;
        this.permissionName = permissionName;
        this.children = children;
    }
}
