import * as React from 'react';
import Arvtakere from '../custom/Arvtakere';

export default function OEDStartside(){
    return(
        <div style={{backgroundColor: "lightblue", height: "200px", width: "500px", padding: "20px"}}>
            <div style={{border: "solid 1px darkgrey", padding:"10px"}}>
                <div style={{fontWeight: "bold"}}>Arvtakere</div>
                <Arvtakere></Arvtakere>
            </div>
            <div style={{border: "solid 1px darkgrey", padding: "10px", marginTop:"10px"}}>Panel 2</div>
        </div>
    );
}