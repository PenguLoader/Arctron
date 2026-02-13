import type { ExecOptions, ExecResult } from "../api/process";
import { getNativeBridge } from "./nativeBridge";

class PlatformProcess {
	exec(command: string, args: string[] = [], options: ExecOptions = {}): Promise<ExecResult> {
		return Promise.resolve(getNativeBridge().processExec(command, args, options));
	}
}

export const process = new PlatformProcess();
