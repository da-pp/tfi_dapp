const FirmaYa = (() => {
    function dialogo(texto, botones) {
        return new Promise((resolver) => {
            const fondo = document.createElement("div");
            fondo.className = "modal";
            fondo.setAttribute("role", "dialog");
            fondo.setAttribute("aria-modal", "true");

            const caja = document.createElement("div");
            caja.className = "modal__caja";
            const parrafo = document.createElement("p");
            parrafo.className = "modal__texto";
            parrafo.textContent = texto;
            const acciones = document.createElement("div");
            acciones.className = "acciones acciones--fin";

            const cerrar = (clave) => {
                fondo.remove();
                document.removeEventListener("keydown", alEscape);
                resolver(clave);
            };
            const alEscape = (evento) => {
                if (evento.key === "Escape") cerrar("cancelar");
            };

            for (const boton of botones) {
                const elemento = document.createElement("button");
                elemento.type = "button";
                elemento.className = `boton boton--chico ${boton.primario ? "boton--primario" : "boton--secundario"}`;
                elemento.textContent = boton.texto;
                elemento.addEventListener("click", () => cerrar(boton.clave));
                acciones.append(elemento);
            }

            caja.append(parrafo, acciones);
            fondo.append(caja);
            document.body.append(fondo);
            document.addEventListener("keydown", alEscape);
            acciones.querySelector(".boton--primario")?.focus();
        });
    }

    function aviso(texto) {
        const elemento = document.createElement("div");
        elemento.className = "aviso";
        elemento.setAttribute("role", "status");
        elemento.textContent = texto;
        document.body.append(elemento);
        setTimeout(() => {
            elemento.classList.add("aviso--oculto");
            elemento.addEventListener("transitionend", () => elemento.remove(), { once: true });
        }, 3000);
    }

    async function copiar(texto, confirmacion) {
        try {
            await navigator.clipboard.writeText(texto);
        } catch {
            const area = document.createElement("textarea");
            area.value = texto;
            document.body.append(area);
            area.select();
            document.execCommand("copy");
            area.remove();
        }
        aviso(confirmacion || "Enlace copiado al portapapeles");
    }

    function marcarCampo(input, mensaje) {
        const contenedor = input.closest(".campo");
        if (!contenedor) return;
        contenedor.classList.toggle("campo--error", Boolean(mensaje));
        input.setAttribute("aria-invalid", mensaje ? "true" : "false");
        const ayuda = contenedor.querySelector(".campo__ayuda");
        if (ayuda) ayuda.textContent = mensaje || "";
    }

    return { dialogo, aviso, copiar, marcarCampo };
})();

(() => {
    document.querySelectorAll("[data-autocerrar]").forEach((aviso) => {
        setTimeout(() => {
            aviso.classList.add("aviso--oculto");
            aviso.addEventListener("transitionend", () => aviso.remove(), { once: true });
        }, 4000);
    });

    document.querySelectorAll("[data-copiar]").forEach((boton) => {
        boton.addEventListener("click", () => FirmaYa.copiar(boton.dataset.copiar, boton.dataset.confirmacion));
    });

    document.querySelectorAll("input[data-fecha]").forEach((input) => {
        input.addEventListener("input", () => {
            const digitos = input.value.replace(/\D/g, "").slice(0, 8);
            const partes = [digitos.slice(0, 2), digitos.slice(2, 4), digitos.slice(4, 8)].filter(Boolean);
            input.value = partes.join("/");
        });
    });

    document.querySelectorAll("textarea[data-contador]").forEach((area) => {
        const nota = document.getElementById(area.dataset.contador);
        const maximo = Number(area.getAttribute("maxlength"));
        const actualizar = () => {
            const restantes = maximo - area.value.length;
            nota.textContent = `${restantes} caracteres restantes`;
            nota.classList.toggle("campo__nota--agotado", restantes <= 0);
        };
        area.addEventListener("input", actualizar);
        actualizar();
    });

    document.querySelectorAll("form[data-validar]").forEach((formulario) => {
        formulario.addEventListener("submit", (evento) => {
            let primero = null;
            formulario.querySelectorAll("[required]").forEach((input) => {
                const vacio = input.type === "radio"
                    ? !formulario.querySelector(`input[name="${input.name}"]:checked`)
                    : !input.value.trim();
                FirmaYa.marcarCampo(input, vacio ? "Este campo es obligatorio" : null);
                if (vacio && !primero) primero = input;
            });
            if (primero) {
                evento.preventDefault();
                evento.stopImmediatePropagation();
                primero.focus();
            }
        });
        formulario.querySelectorAll("[required]").forEach((input) =>
            input.addEventListener("input", () => FirmaYa.marcarCampo(input, null)));
    });

    document.querySelectorAll("form[data-confirmar]").forEach((formulario) => {
        formulario.addEventListener("submit", async (evento) => {
            if (formulario.dataset.confirmado === "si") return;
            evento.preventDefault();
            const texto = formulario.dataset.confirmar.replace(/\{(\w+)\}/g, (_, nombre) => {
                const campo = formulario.elements[nombre];
                return campo?.selectedOptions?.[0]?.text ?? campo?.value ?? "";
            });
            const eleccion = await FirmaYa.dialogo(texto, [
                { clave: "cancelar", texto: "Cancelar" },
                { clave: "confirmar", texto: "Confirmar", primario: true },
            ]);
            if (eleccion === "confirmar") {
                formulario.dataset.confirmado = "si";
                formulario.requestSubmit(evento.submitter);
            }
        });
    });
})();
