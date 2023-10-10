import React from "react";
import { Dropdown } from 'office-ui-fabric-react/lib/Dropdown';

export default class RecordConstraint extends React.Component {
    constructor(props) {
        super(props);
        this.recordingContentConstraints = [
            { key: 'audio', text: 'audio' },
        ];
        this.recordingChannelConstraints = [
            { key: 'mixed', text: 'mixed' },
            { key: 'unmixed', text: 'unmixed' },
        ];
        this.recordingFormatConstraints = [
            { key: 'wav', text: 'wav' },
            { key: 'mp3', text: 'mp3' },
        ];
        this.state = {
            recordingContent: 'audio',
            recordingChannel: 'unmixed',
            recordingFormat: 'wav',
        };
    }

    handleChange = async (event, item) => {
        const recordConstraints = {
            recordingContent: this.state.recordingContent,
            recordingChannel: this.state.recordingChannel,
            recordingFormat: this.state.recordingFormat
        };

        if (event.target.id === 'recordingContentDropdown') {
            recordConstraints.recordingContent = item.key;
            if (item.key === 'audio') {
                recordConstraints.recordingChannel = 'mixed';
            } else if (item.key === 'unmixed') {
                recordConstraints.recordingChannel = 'unmixed';
                recordConstraints.recordingFormat = 'wav';
            }
        } else if (event.target.id === 'recordingChannelDropdown') {
            recordConstraints.recordingChannel = item.key;
            if (item.key === 'mixed') {
                recordConstraints.recordingFormat = 'wav';
            }
            if (item.key === 'unmixed') {
                recordConstraints.recordingFormat = 'wav';
            }
        } else if (event.target.id === 'recordingFormatDropdown') {
            recordConstraints.recordingFormat = item.key;
        }

        this.setState(recordConstraints);

        if (this.props.onChange) {
            this.props.onChange(recordConstraints);
        }
    }

    render() {
        const formatOptions = this.state.recordingContent === 'audio' && this.state.recordingChannel === 'mixed'
            ? this.recordingFormatConstraints
            : [{ key: 'wav', text: 'wav' }];

        return (
            <div>
                <Dropdown
                    id='recordingContentDropdown'
                    selectedKey={this.state.recordingContent}
                    onChange={this.handleChange}
                    label={'Send Recording Content'}
                    options={this.recordingContentConstraints}
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
                    options={formatOptions}
                    styles={{ dropdown: { width: 200 }, label: { color: '#FFF' } }}
                    disabled={this.props.disabled}
                />
            </div>
        );
    }
}
