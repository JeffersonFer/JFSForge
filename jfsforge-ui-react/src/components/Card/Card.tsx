import type { ReactNode } from "react";
import styles from "./Card.module.css";

type CardProps = {
  titulo: string;
  children: ReactNode;
};

export function Card({ titulo, children }: CardProps) {
  return (
    <div className={styles.card}>
      <h2 className={styles.titulo}>{titulo}</h2>
      <div className={styles.conteudo}>{children}</div>
    </div>
  );
}