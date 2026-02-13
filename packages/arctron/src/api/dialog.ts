export interface OpenDialogOptions {
  title?: string;
  allowMultiple?: boolean;
  filters?: { name: string; extensions: string[] }[];
}

export interface SaveDialogOptions {
  title?: string;
  defaultPath?: string;
  filters?: { name: string; extensions: string[] }[];
}

export interface MessageBoxOptions {
  title?: string;
  message: string;
  detail?: string;
  buttons?: string[];
}

export interface MessageBoxResult {
  response: number;
}
