(() => {
    const formulario = document.getElementById("form-login");
    if (!formulario) return;

    const TIEMPO_ESPERA_MS = 15000;
    const LONGITUD_MAXIMA_CORREO = 254;
    const LONGITUD_MINIMA_CONTRASENA = 8;
    const FORMATO_CORREO = /^[^@\s]+@[^@\s]+\.[^@\s]+$/;
    const MENSAJE_ERROR_CONEXION = "No se pudo conectar al servidor. Verifica tu conexión a internet.";

    const campos = {
        Correo: document.getElementById("correo"),
        Contrasena: document.getElementById("contrasena"),
    };
    const botonEnviar = formulario.querySelector('button[type="submit"]');
    const alerta = document.getElementById("alerta");
    const alertaTexto = alerta.querySelector(".alerta__texto");
    const alertaAcciones = alerta.querySelector(".alerta__acciones");

    function validar() {
        const errores = {};
        const correo = campos.Correo.value.trim();
        const contrasena = campos.Contrasena.value;

        if (!correo) errores.Correo = "El campo Email es obligatorio";
        else if (correo.length > LONGITUD_MAXIMA_CORREO || !FORMATO_CORREO.test(correo))
            errores.Correo = "Ingrese un correo electrónico válido";

        if (!contrasena) errores.Contrasena = "El campo Contraseña es obligatorio";
        else if (contrasena.length < LONGITUD_MINIMA_CONTRASENA)
            errores.Contrasena = `La contraseña debe tener al menos ${LONGITUD_MINIMA_CONTRASENA} caracteres`;

        return errores;
    }

    function marcarCampo(input, mensaje) {
        const contenedor = input.closest(".campo");
        const ayuda = document.getElementById(input.getAttribute("aria-describedby"));
        contenedor.classList.toggle("campo--error", Boolean(mensaje));
        input.setAttribute("aria-invalid", mensaje ? "true" : "false");
        ayuda.textContent = mensaje || "";
    }

    function mostrarErrores(errores) {
        let primero = null;
        for (const [nombre, input] of Object.entries(campos)) {
            marcarCampo(input, errores[nombre]);
            if (errores[nombre] && !primero) primero = input;
        }
        if (primero) primero.focus();
    }

    function mostrarAlerta(mensaje, { bloqueada = false, conexion = false } = {}) {
        alerta.className = bloqueada ? "alerta alerta--advertencia" : "alerta alerta--error";
        alertaTexto.textContent = mensaje;
        alertaAcciones.hidden = !conexion;
        alerta.hidden = false;
    }

    function ocultarAlerta() {
        alerta.hidden = true;
    }

    async function enviar() {
        ocultarAlerta();
        const errores = validar();
        mostrarErrores(errores);
        if (Object.keys(errores).length > 0) return;

        botonEnviar.disabled = true;
        const controlador = new AbortController();
        const temporizador = setTimeout(() => controlador.abort(), TIEMPO_ESPERA_MS);

        try {
            const respuesta = await fetch(window.location.href, {
                method: "POST",
                body: new FormData(formulario),
                headers: { Accept: "application/json" },
                credentials: "same-origin",
                signal: controlador.signal,
            });
            if (!respuesta.ok) throw new Error(`HTTP ${respuesta.status}`);

            const datos = await respuesta.json();
            if (datos.exito) {
                window.location.assign(datos.redireccion);
                return;
            }
            if (datos.errores) mostrarErrores(datos.errores);
            if (datos.mensaje) {
                mostrarAlerta(datos.mensaje, { bloqueada: datos.cuentaBloqueada, conexion: datos.errorConexion });
            }
        } catch {
            mostrarAlerta(MENSAJE_ERROR_CONEXION, { conexion: true });
        } finally {
            clearTimeout(temporizador);
            botonEnviar.disabled = false;
        }
    }

    formulario.addEventListener("submit", (evento) => {
        evento.preventDefault();
        enviar();
    });

    campos.Correo.addEventListener("keydown", (evento) => {
        if (evento.key === "Enter" && !campos.Contrasena.value) {
            evento.preventDefault();
            campos.Contrasena.focus();
        }
    });

    for (const input of Object.values(campos)) {
        input.addEventListener("input", () => marcarCampo(input, null));
    }

    alerta.querySelector('[data-accion="reintentar"]').addEventListener("click", enviar);
    alerta.querySelector('[data-accion="cancelar"]').addEventListener("click", ocultarAlerta);
})();
