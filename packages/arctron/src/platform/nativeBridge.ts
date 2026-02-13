import type { BrowserWindowOptions } from "../api/browserWindow";
import type { TrayEvent, TrayMenuItem, TrayOptions } from "../api/tray";
import type { OpenDialogOptions, SaveDialogOptions, MessageBoxOptions, MessageBoxResult } from "../api/dialog";
import type { ExecOptions, ExecResult } from "../api/process";

export interface NativeBridge {
  appQuit(): void;

  windowCreate(options: BrowserWindowOptions): number;
  windowLoadUrl(id: number, url: string): void;
  windowShow(id: number): void;
  windowHide(id: number): void;
  windowClose(id: number): void;
  windowSetTitle(id: number, title: string): void;
  windowGetSize(id: number): number[];
  windowGetPosition(id: number): number[];
  windowFocus(id: number): void;
  windowCenter(id: number): void;
  windowMinimize(id: number): void;
  windowUnminimize(id: number): void;
  windowIsMinimized(id: number): boolean;
  windowMaximize(id: number): void;
  windowUnmaximize(id: number): void;
  windowIsMaximized(id: number): boolean;
  windowSetRpcManifest(id: number, rpcNamespace: string | null, methods: string[]): void;
  windowRpcResolve(id: number, requestId: string, resultJson: string): void;
  windowRpcReject(id: number, requestId: string, error: string): void;

  trayCreate(options: TrayOptions): number;
  traySetToolTip(id: number, tooltip: string): void;
  traySetMenu(id: number, menu: TrayMenuItem[]): void;
  trayOnEvent(handler: (trayId: number, event: TrayEvent, menuItemId?: string) => void): void;

  dialogOpenFile(options: OpenDialogOptions): string[];
  dialogSaveFile(options: SaveDialogOptions): string | null;
  dialogMessageBox(options: MessageBoxOptions): MessageBoxResult;

  shellOpenExternal(url: string): void;
  shellOpenPath(path: string): void;
  shellRevealPath(path: string): void;
  shellTrashPath(path: string): void;

  pathJoin(parts: string[]): string;
  pathResolve(parts: string[]): string;
  pathGetUserData(): string;
  pathGetLocalData(): string;
  pathGetTemp(): string;
  pathGetCwd(): string;
  pathGetBaseExeDir(): string;

  fsReadText(path: string): string;
  fsWriteText(path: string, content: string): void;
  fsAppendText(path: string, content: string): void;
  fsExists(path: string): boolean;
  fsMkdir(path: string, recursive: boolean): void;
  fsReadDir(path: string): string[];
  fsRemove(path: string, recursive: boolean): void;

  processExec(command: string, args: string[], options: ExecOptions): ExecResult;
}

export function getNativeBridge(): NativeBridge {
  const native = (globalThis as { __arctronNative?: NativeBridge }).__arctronNative;
  if (!native) {
    throw new Error("Native bridge is not available");
  }
  return native;
}
