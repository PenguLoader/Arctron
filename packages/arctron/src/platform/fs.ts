import type { FsApi } from "../api/fs";
import { getNativeBridge } from "./nativeBridge";

class PlatformFs implements FsApi {
  readText(path: string): Promise<string> {
    return Promise.resolve(getNativeBridge().fsReadText(path));
  }

  writeText(path: string, content: string): Promise<void> {
    getNativeBridge().fsWriteText(path, content);
    return Promise.resolve();
  }

  appendText(path: string, content: string): Promise<void> {
    getNativeBridge().fsAppendText(path, content);
    return Promise.resolve();
  }

  exists(path: string): Promise<boolean> {
    return Promise.resolve(getNativeBridge().fsExists(path));
  }

  mkdir(path: string, recursive = true): Promise<void> {
    getNativeBridge().fsMkdir(path, recursive);
    return Promise.resolve();
  }

  readdir(path: string): Promise<string[]> {
    return Promise.resolve(getNativeBridge().fsReadDir(path));
  }

  rm(path: string, recursive = false): Promise<void> {
    getNativeBridge().fsRemove(path, recursive);
    return Promise.resolve();
  }
}

export const fs = new PlatformFs();
