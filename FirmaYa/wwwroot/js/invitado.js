(() => {
    const vista = document.getElementById("vista-contrato");
    if (!vista) return;

    const minuto = 60 * 1000;
    const inactividad = Number(vista.dataset.minutosInactividad) * minuto;
    const margen = Number(vista.dataset.minutosAviso) * minuto;
    const token = vista.querySelector('input[name="__RequestVerificationToken"]').value;
    let temporizador;
    let avisoAbierto = false;

    function expirar() {
        const plantilla = document.getElementById("pantalla-expirada");
        vista.replaceWith(plantilla.content.cloneNode(true));
        document.querySelector(".modal")?.remove();
        clearTimeout(temporizador);
        ["mousemove", "keydown", "scroll", "click", "touchstart"].forEach((evento) =>
            window.removeEventListener(evento, reiniciar));
    }

    async function avisar() {
        avisoAbierto = true;
        const vencimiento = setTimeout(expirar, margen);
        const eleccion = await FirmaYa.dialogo("Su sesión está por expirar en 5 minutos. ¿Desea continuar?", [
            { clave: "salir", texto: "Salir" },
            { clave: "continuar", texto: "Continuar", primario: true },
        ]);
        clearTimeout(vencimiento);
        avisoAbierto = false;

        if (eleccion !== "continuar") {
            expirar();
            return;
        }
        try {
            const datos = new FormData();
            datos.append("__RequestVerificationToken", token);
            const respuesta = await fetch(vista.dataset.renovar, { method: "POST", body: datos });
            const resultado = await respuesta.json();
            if (resultado.renovado) reiniciar(); else expirar();
        } catch {
            expirar();
        }
    }

    function reiniciar() {
        if (avisoAbierto) return;
        clearTimeout(temporizador);
        temporizador = setTimeout(avisar, inactividad);
    }

    ["mousemove", "keydown", "scroll", "click", "touchstart"].forEach((evento) =>
        window.addEventListener(evento, reiniciar, { passive: true }));
    reiniciar();
})();
