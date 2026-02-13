export interface ExecOptions {
  cwd?: string;
  env?: Record<string, string>;
  timeoutMs?: number;
}

export interface ExecResult {
  exitCode: number;
  stdout: string;
  stderr: string;
}

export interface ProcessExec {
  exec(command: string, args?: string[], options?: ExecOptions): Promise<ExecResult>;
}
