export default {
    axios: null,
    //! on_layers_toggled =======================
    get_layers(cb) {
        this.axios
            .get("/on_layers_toggled")
            .then((response) => {
                cb(response.data);
            })
            .catch((error) => {
                console.log(error);
                cb([]);
            });
    },
    get_on_layers(cb) {
        this.axios
            .post("/on_layers_toggled", null)
            .then((response) => {
                cb(response.data);
            })
            .catch((error) => {
                console.log(error);
                cb([]);
            });
    },
    on_layers_toggled(d, cb) {
        this.axios
            .post("/on_layers_toggled", d)
            .then(function (response) {
                cb(response.data);
            })
            .catch(function (error) {
                console.log(error);
                cb([]);
            });
    },
    //! on_options_toggled =======================
    get_options(cb) {
        this.axios
            .get("/on_options_toggled")
            .then((response) => {
                cb(response.data);
            })
            .catch((error) => {
                console.log(error);
                cb([]);
            });
    },
    get_on_options(cb) {
        this.axios
            .post("/on_options_toggled", null)
            .then((response) => {
                cb(response.data);
            })
            .catch((error) => {
                console.log(error);
                cb([]);
            });
    },
    on_options_toggled(d, cb) {
        this.axios
            .post("/on_options_toggled", d)
            .then(function (response) {
                cb(response.data);
            })
            .catch(function (error) {
                console.log(error);
                cb([]);
            });
    }
}