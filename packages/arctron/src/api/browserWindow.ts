export type BrowserWindowRpcHandler = (...args: unknown[]) => Promise<unknown> | unknown;

export interface BrowserWindowRpcOptions {
  namespace?: string;
  methods?: Record<string, BrowserWindowRpcHandler>;
}

export interface BrowserWindowOptions {
  width?: number;
  height?: number;
  x?: number;
  y?: number;
  title?: string;
  show?: boolean;
  frameless?: boolean;
  initialUrl?: string;
  devTools?: boolean;
  contextMenu?: boolean;
  rpc?: BrowserWindowRpcOptions;
}
