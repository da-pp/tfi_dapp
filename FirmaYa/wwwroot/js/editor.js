// CU-02 Editar contrato en línea: barra de formato, borrador local, validación y salida sin guardar.
(() => {
    const formulario = document.getElementById("form-editor");
    if (!formulario) return;

    const MINIMO = 100;
    const area = document.getElementById("contenido");
    const pildora = document.getElementById("cambios-sin-guardar");
    const clave = formulario.dataset.borrador;
    const original = area.value;
    let enviando = false;

    const leerBorrador = () => { try { return localStorage.getItem(clave); } catch { return null; } };
    const guardarBorrador = (texto) => { try { localStorage.setItem(clave, texto); } catch { /* sin almacenamiento */ } };
    const borrarBorrador = () => { try { localStorage.removeItem(clave); } catch { /* sin almacenamiento */ } };

    const hayCambios = () => area.value !== original;
    const actualizarEstado = () => { pildora.hidden = !hayCambios(); };

    // Después de guardar una versión, el borrador de esa versión ya no sirve.
    if (formulario.dataset.guardado) borrarBorrador();

    // Paso 10: los cambios se registran en tiempo real sin persistirlos (borrador local del navegador).
    const borrador = leerBorrador();
    if (borrador !== null && borrador !== original && !formulario.dataset.guardado) {
        area.value = borrador;
        document.getElementById("borrador-recuperado").hidden = false;
    }
    actualizarEstado();

    area.addEventListener("input", () => {
        if (hayCambios()) guardarBorrador(area.value); else borrarBorrador();
        actualizarEstado();
        FirmaYa.marcarCampo(area, null);
    });

    // Barra de formato: marcas livianas que luego se muestran con formato y se limpian en el PDF.
    const formatos = {
        negrita: (texto) => `**${texto || "texto"}**`,
        cursiva: (texto) => `_${texto || "texto"}_`,
        lista: (texto) => prefijarLineas(texto || "elemento", "- "),
        titulo: (texto) => prefijarLineas(texto || "TÍTULO", "# "),
        cita: (texto) => prefijarLineas(texto || "cita", "> "),
    };
    const prefijarLineas = (texto, prefijo) => texto.split("\n").map((linea) => prefijo + linea).join("\n");

    formulario.querySelectorAll("[data-formato]").forEach((boton) => {
        boton.addEventListener("click", () => {
            const { selectionStart: inicio, selectionEnd: fin, value } = area;
            const reemplazo = formatos[boton.dataset.formato](value.slice(inicio, fin));
            area.setRangeText(reemplazo, inicio, fin, "end");
            area.focus();
            area.dispatchEvent(new Event("input"));
        });
    });

    // Pasos 14 y 15: el cuerpo no puede estar vacío y debe tener al menos 100 caracteres.
    formulario.addEventListener("submit", (evento) => {
        if (area.value.trim().length < MINIMO) {
            evento.preventDefault();
            FirmaYa.marcarCampo(area, "El contenido del contrato debe tener al menos 100 caracteres");
            area.focus();
            return;
        }
        enviando = true;
    });

    // Camino alternativo "Usuario intenta salir sin guardar".
    document.addEventListener("click", async (evento) => {
        const enlace = evento.target.closest("a[href]");
        if (!enlace || !hayCambios() || enlace.target === "_blank") return;
        evento.preventDefault();

        const eleccion = await FirmaYa.dialogo("¿Desea guardar los cambios antes de salir?", [
            { clave: "cancelar", texto: "Cancelar" },
            { clave: "descartar", texto: "Descartar" },
            { clave: "guardar", texto: "Guardar", primario: true },
        ]);
        if (eleccion === "guardar") {
            formulario.requestSubmit();
        } else if (eleccion === "descartar") {
            borrarBorrador();
            enviando = true;
            window.location.href = enlace.href;
        }
    });

    window.addEventListener("beforeunload", (evento) => {
        if (hayCambios() && !enviando) evento.preventDefault();
    });
})();
