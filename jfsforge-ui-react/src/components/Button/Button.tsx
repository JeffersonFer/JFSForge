import styles from "./Button.module.css";

type ButtonProps = {
    children: React.ReactNode;
    onClick?: () => void;
    disabled?: boolean;
}

export function Button({ children, onClick, disabled }: ButtonProps) {
    return (
        <button className={styles.button} onClick={onClick} disabled={disabled}>
            {children}
        </button>
    );
}