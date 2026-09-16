import { Button } from "./components/Button/Button";
import "./App.css";

function App() {
  return (
    <div style={{ padding: "2rem", fontFamily: "Segoe UI, sans-serif" }}>
      <h1>Renumerador de Elevações</h1>
      <Button onClick={() => console.log("clicou")}>Selecionar Elevações</Button>
      <Button onClick={() => console.log("clicou")}>Processar</Button>
      <Button disabled>Desabilitado</Button>
    </div>
  );
}

export default App;