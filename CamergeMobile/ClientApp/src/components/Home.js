import React, { Component } from 'react';
import { WebView } from 'react';

export class Home extends React.Component {
    static displayName = Home.name;
    render() {
        window.location.replace('https://gestao.camerge.com.br/Admin/')
    }
}



