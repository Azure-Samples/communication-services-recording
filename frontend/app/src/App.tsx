import React from 'react';
import './styles/App.css';
import { MakeCall } from './components/Call/MakeCall';

function App() {
    return (
        <div className="main-app" >
            <div className="wrapper">
                <MakeCall/>
            </div>
            
        </div>
    );
}

export default App;
