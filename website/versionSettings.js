// @ts-check
import fs from "node:fs";

function getAvaloniaVersion (){
    try {
        return fs.readFileSync('./AvaloniaVersion.txt', {encoding: 'utf8'});
    }
    catch (e) {
        console.error(e);
    }
}

export const versionSettings = {
    current: getAvaloniaVersion(),
};