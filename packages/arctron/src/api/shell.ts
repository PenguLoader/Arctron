export interface Shell {
  openURL(url: string): Promise<void>;
  revealPath(path: string): Promise<void>;
  trashPath(path: string): Promise<void>;
  openExternal(url: string): Promise<void>;
  openPath(path: string): Promise<void>;
}
