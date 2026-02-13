export interface TrayMenuItem {
  id: string;
  label: string;
  enabled?: boolean;
}

export interface TrayOptions {
  tooltip?: string;
  icon?: string;
  menu?: TrayMenuItem[];
}

export type TrayEvent = "click" | "double-click" | "menu-item-click";
