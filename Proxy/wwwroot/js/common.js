class Api{
	constructor() {

    }
	
    PostJson(url, object) {
        return new Promise((resolve, reject) => {
            $.ajax(url, {
                method: "POST",
                data: JSON.stringify(object),
                content: 'json',
                contentType: "application/json; charset=utf-8",
                success: (data) => {
                    resolve(data);
                },
                error: (data) => {
                    reject(data);
                }
            });
        });
    }
	
	async ExportProxies() {
        return await this.PostJson('/api/proxydata/export', null);
    }
}

let api = new Api();