export default {
    axiso: null,
    get_layers(cb) {
        this.axiso
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
        this.axiso
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
        this.axiso
            .post("/on_layers_toggled", d)
            .then(function (response) {
                cb(response.data);
            })
            .catch(function (error) {
                console.log(error);
                cb([]);
            });
    }
}