import { useState } from "react";
import { Button } from "../../components/Button/Button";
import { TextInput } from "../../components/TextInput/TextInput";
import styles from "./Renumerador.module.css";

export function Renumerador() {
  const [prefixo, setPrefixo] = useState("PAR");
  const [valorInicial, setValorInicial] = useState("1");

  const camposPreenchidos = prefixo.trim() !== "" && valorInicial.trim() !== "";

  const handleSelecionarElevacoes = () => {
    console.log("Selecionar Elevações clicado");
  };

  const handleProcessar = () => {
    const mensagem = {
      tipo: "processar",
      prefixo: prefixo,
      valorInicial: valorInicial,
    };

    if (window.chrome?.webview) {
      window.chrome.webview.postMessage(mensagem);
    } else {
      console.log("Mensagem (sem WebView2):", mensagem);
    }
  };

  return (
    <div className={styles.container}>
      <h1 className={styles.titulo}>Renumerador de Elevações</h1>

      <Button onClick={handleSelecionarElevacoes}>Selecionar Elevações</Button>

      <div className={styles.campos}>
        <TextInput
          label="Prefixo"
          value={prefixo}
          onChange={setPrefixo}
          placeholder="PAR"
        />
        <TextInput
          label="Valor Inicial"
          value={valorInicial}
          onChange={setValorInicial}
        />
      </div>

      <Button onClick={handleProcessar} disabled={!camposPreenchidos}>
        Processar
      </Button>
    </div>
  );
}