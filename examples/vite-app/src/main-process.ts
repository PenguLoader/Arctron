import { app, BrowserWindow } from "arctron";

app.whenReady().then(() => {
  const win = new BrowserWindow({
    width: 1200,
    height: 800,
    title: "Arctron"
  });

  win.loadURL("http://localhost:5173");
});
