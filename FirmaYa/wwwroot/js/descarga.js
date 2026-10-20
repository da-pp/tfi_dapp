// CU-10 pasos 4 y 13 a 16, y camino "Error en la generación del PDF" (Reintentar / Cancelar).
(() => {
    const seccion = document.getElementById("descarga");
    const enlace = seccion?.querySelector("[data-descargar]");
    if (!enlace) return;

    const estado = document.getElementById("estado-descarga");
    const reintentar = seccion.querySelector("[data-reintentar]");
    const cancelar = seccion.querySelector("[data-cancelar]");

    function mostrar(texto, tipo) {
        estado.textContent = texto;
        estado.className = `alerta ${tipo}`;
        estado.hidden = false;
    }

    function botonesError(visibles) {
        reintentar.hidden = !visibles;
        cancelar.hidden = !visibles;
    }

    async function descargar() {
        botonesError(false);
        mostrar("Generando PDF...", "alerta--info");
        enlace.setAttribute("aria-disabled", "true");
        try {
            const respuesta = await fetch(enlace.href, { credentials: "same-origin" });
            if (!respuesta.ok) throw new Error(await respuesta.text());

            const archivo = await respuesta.blob();
            const url = URL.createObjectURL(archivo);
            const vinculo = document.createElement("a");
            vinculo.href = url;
            vinculo.download = seccion.dataset.nombre;
            document.body.append(vinculo);
            vinculo.click();
            vinculo.remove();
            setTimeout(() => URL.revokeObjectURL(url), 10000);
            mostrar("El PDF se descargó exitosamente.", "alerta--exito");
        } catch {
            mostrar("El PDF no pudo ser generado en este momento.", "alerta--error");
            botonesError(true);
        } finally {
            enlace.removeAttribute("aria-disabled");
        }
    }

    enlace.addEventListener("click", (evento) => {
        evento.preventDefault();
        descargar();
    });
    reintentar.addEventListener("click", descargar);
    cancelar.addEventListener("click", () => {
        estado.hidden = true;
        botonesError(false);
    });
})();
