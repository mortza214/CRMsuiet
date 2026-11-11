import React from "react";
import ReactDOM from "react-dom/client";
import "antd/dist/reset.css";
import dayjs from "dayjs";
import "dayjs/locale/fa";
import { App } from "@/App";
import "@/styles/global.css";

dayjs.locale("fa");

ReactDOM.createRoot(document.getElementById("root")!).render(
  <React.StrictMode>
    <App />
  </React.StrictMode>,
);
