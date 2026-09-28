import { HashRouter, Routes, Route } from "react-router-dom";
import { Renumerador } from "./pages/Renumerador/Renumerador";
import { AplicadorDeTags } from "./pages/AplicadorDeTags/AplicadorDeTags";

function App() {
  return (
    <HashRouter>
      <Routes>
        <Route path="/renumerador" element={<Renumerador />} />
        <Route path="/aplicador-de-tags" element={<AplicadorDeTags />} />
      </Routes>
    </HashRouter>
  );
}

export default App;