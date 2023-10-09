import React from "react";
import { Dropdown } from 'office-ui-fabric-react/lib/Dropdown';

export default class RecordConstraint extends React.Component {
    constructor(props) {
        super(props);
        this.recordingContentContraints = [
            { key: 'audio', text: 'audio' },
            
        ];
        this.recordingChannelConstraints = [
            { key: 'unmixed', text: 'unmixed' },
            
        ];
        this.recordingFormatContraints = [
            { key: 'wav', text: 'wav' },
            
        ];
        this.state = {
            recordingContent: 'audio',
            recordingChannel: 'unmixed',
            recordingFormat: 'wav'
        }
    }

    handleChange = async (event, item) => {
        const recordConstraints = {
            recordingContent : this.state.recordingContent,
            recordingChannel : this.state.recordingChannel,
            recordingFormat : this.state.recordingFormat
        };

        if (event.target.id === 'recordingContentDropdown') {
            recordConstraints.recordingContent = item.key;
            this.setState({
                recordingContent: item.key
            });
        } else if (event.target.id === 'recordingChannelDropdown') {
            recordConstraints.recordingChannel = item.key;
            this.setState({
                recordingChannel: item.key
            });
        } else if (event.target.id === 'recordingFormatDropdown') {
            recordConstraints.recordingFormat = item.key;
            this.setState({
                recordingFormat: item.key
            });
        }

        if (this.props.onChange) {
            this.props.onChange(recordConstraints);
        }
    }

    render() {
        return (
            <div>
                <Dropdown
                    id='recordingContentDropdown'
                    selectedKey={this.state.recordingContent}
                    onChange={this.handleChange}
                    label={'Send Recording Content'}
                    options={this.recordingContentContraints}
                    styles={{ dropdown: { width: 200 }, label: { color: '#FFF' } }}
                    disabled={this.props.disabled}
                />
                <Dropdown
                    id='recordingChannelDropdown'
                    selectedKey={this.state.recordingChannel}
                    onChange={this.handleChange}
                    label={'Send Recording Channel'}
                    options={this.recordingChannelConstraints}
                    styles={{ dropdown: { width: 200 }, label: { color: '#FFF' } }}
                    disabled={this.props.disabled}
                />
                <Dropdown
                    id='recordingFormatDropdown'
                    selectedKey={this.state.recordingFormat}
                    onChange={this.handleChange}
                    label={'Send Recording Format'}
                    options={this.recordingFormatContraints}
                    styles={{ dropdown: { width: 200 }, label: { color: '#FFF' } }}
                    disabled={this.props.disabled}
                />
            </div>
        );
    }
}

